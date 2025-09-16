// 폴드식(접는) 에디터 + Parent 지정/변경 지원
// Unity 2021+ / 6000.x 이상

using Codice.Client.BaseCommands;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[CustomEditor(typeof(BodyDefinitionSO))]
public class BodyDefinitionSO_FoldEditor : Editor
{
    BodyDefinitionSO _def;

    // 폴드 상태는 세션 단위 유지
    static bool GetFold(BodyPartSO part) =>
        SessionState.GetBool($"BodyFold_{part.GetInstanceID()}", true);
    static void SetFold(BodyPartSO part, bool v) =>
        SessionState.SetBool($"BodyFold_{part.GetInstanceID()}", v);

    void OnEnable()
    {
        _def = (BodyDefinitionSO)target;
    }
    void Refresh()
    {
        if (_def == null) return;
        Undo.IncrementCurrentGroup();
        var group = Undo.GetCurrentGroup();

        // 1. 모든 유효한(계층도에 연결된) 파트 수집
        var liveParts = new HashSet<BodyPartSO>();
        if (_def.roots != null)
        {
            // 먼저 루트 목록에서 null 항목 정리
            if (_def.roots.RemoveAll(item => item == null) > 0)
            {
                Undo.RecordObject(_def, "Remove Null Roots");
                EditorUtility.SetDirty(_def);
            }

            foreach (var root in _def.roots)
            {
                CollectLiveParts(root, liveParts);
            }
        }

        // 2. 모든 파트의 부모 참조를 최신 계층도에 맞게 업데이트
        UpdateAllParentReferences();

        // 3. 에셋 파일에 포함된 모든 BodyPartSO 서브에셋 로드
        var path = AssetDatabase.GetAssetPath(_def);
        var allSubAssets = AssetDatabase.LoadAllAssetsAtPath(path);
        var allPartsInAsset = new List<BodyPartSO>();
        foreach (var subAsset in allSubAssets)
        {
            if (subAsset is BodyPartSO part && part != null)
            {
                allPartsInAsset.Add(part);
            }
        }

        // 4. 참조되지 않는(고아) 파트 찾아서 삭제
        var changed = false;
        foreach (var partInAsset in allPartsInAsset)
        {
            if (!liveParts.Contains(partInAsset))
            {
                Debug.Log($"[Refresh] Removing orphaned BodyPartSO: {partInAsset.name}", _def);
                Undo.DestroyObjectImmediate(partInAsset);
                changed = true;
            }
        }

        if (changed)
        {
            AssetDatabase.SaveAssets();
            Debug.Log($"[Refresh] Found and removed orphaned parts.", _def);
        }
        else
        {
            Debug.Log("[Refresh] No orphaned parts found.", _def);
        }
        
        Undo.SetCurrentGroupName("Refresh Body Definition");
        Undo.CollapseUndoOperations(group);
    }

    void UpdateAllParentReferences()
    {
        if (_def == null) return;

        // 1. (자식 -> 부모 리스트) 맵을 새로 빌드합니다.
        var childToParentsMap = new Dictionary<BodyPartSO, List<BodyPartSO>>();
        var allParts = new HashSet<BodyPartSO>(); // 순환 구조 방지를 위해 방문한 파트 기록
        if (_def.roots != null)
        {
            foreach(var root in _def.roots)
            {
                // 루트부터 시작하여 재귀적으로 맵 빌드
                BuildChildToParentsMapRecursive(root, childToParentsMap, allParts);
            }
        }

        // 2. 맵을 기준으로 모든 파트의 부모 리스트를 동기화합니다.
        // allParts 에는 모든 live part가 들어있음.
        foreach (var part in allParts)
        {
            // 맵에서 계산된 '올바른' 부모 리스트
            var correctParents = childToParentsMap.ContainsKey(part) 
                ? childToParentsMap[part] 
                : new List<BodyPartSO>();
            
            // 현재 파트에 할당된 부모 리스트
            var currentParents = part.parent ?? new List<BodyPartSO>();
            
            // 두 리스트가 동일한지 순서에 상관없이 비교
            var areSame = new HashSet<BodyPartSO>(currentParents).SetEquals(correctParents);

            if (!areSame)
            {
                Undo.RecordObject(part, "Sync Parent References");
                // 가장 간단하고 확실한 방법: 계산된 리스트로 덮어쓰기
                part.parent.Clear();
                part.parent.AddRange(correctParents);
                EditorUtility.SetDirty(part);
            }
        }
    }

    void BuildChildToParentsMapRecursive(BodyPartSO part, Dictionary<BodyPartSO, List<BodyPartSO>> map, HashSet<BodyPartSO> visited)
    {
        if (part == null || !visited.Add(part))
        {
            return; // 이미 방문했으면 종료 (순환 구조 방지)
        }

        // 자식들을 순회하며 부모-자식 관계를 맵에 기록
        if (part.children != null)
        {
            foreach (var child in part.children)
            {
                if (child == null) continue;

                if (!map.ContainsKey(child))
                {
                    map[child] = new List<BodyPartSO>();
                }
                map[child].Add(part);

                // 각 자식에 대해 재귀 호출
                BuildChildToParentsMapRecursive(child, map, visited);
            }
        }
    }

    void CollectLiveParts(BodyPartSO part, HashSet<BodyPartSO> liveParts)
    {
        if (part == null || !liveParts.Add(part))
        {
            return;
        }

        if (part.children != null)
        {
            // 자식 목록에서 null 항목 정리
            if (part.children.RemoveAll(item => item == null) > 0)
            {
                Undo.RecordObject(part, "Remove Null Children");
                EditorUtility.SetDirty(part);
            }

            foreach (var child in part.children)
            {
                CollectLiveParts(child, liveParts);
            }
        }
    }
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            if (GUILayout.Button("Add Root", EditorStyles.toolbarButton, GUILayout.Width(90)))
                AddRoot();

            if (GUILayout.Button("Expand All", EditorStyles.toolbarButton, GUILayout.Width(90)))
                VisitAll((p) => SetFold(p, true));
            if (GUILayout.Button("Collapse All", EditorStyles.toolbarButton, GUILayout.Width(90)))
                VisitAll((p) => SetFold(p, false));
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(90)))
                Refresh();
            GUILayout.FlexibleSpace();
        }

        EditorGUILayout.Space(4);

        if (_def.roots == null || _def.roots.Count == 0)
        {
            EditorGUILayout.HelpBox("루트를 추가하세요. (Add Root)", MessageType.Info);
        }
        else
        {
            for (int i = 0; i < _def.roots.Count; i++)
            {
                var part = _def.roots[i];
                if (part == null)
                {
                    using (new EditorGUILayout.HorizontalScope("box"))
                    {
                        EditorGUILayout.HelpBox("NULL Root", MessageType.Warning);
                        if (GUILayout.Button("Remove", GUILayout.Width(70)))
                        {
                            Undo.RecordObject(_def, "Remove Null Root");
                            _def.roots.RemoveAt(i--);
                            EditorUtility.SetDirty(_def);
                        }
                    }
                    continue;
                }

                DrawPartRecursive(part, parent: null, siblingList: _def.roots, indexInParent: i);
                EditorGUILayout.Space(2);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    // 재귀 폴드 렌더링
    void DrawPartRecursive(BodyPartSO part, BodyPartSO parent, List<BodyPartSO> siblingList, int indexInParent)
    {
        if (part == null) return;

        // 헤더 바
        var headerRect = GUILayoutUtility.GetRect(0, 22, GUILayout.ExpandWidth(true));
        var toggleRect = new Rect(headerRect.x + 6, headerRect.y + 3, 16, 16);
        var buttonsW = 220f;
        var labelRect = new Rect(toggleRect.xMax + 6, headerRect.y + 2,
                                  headerRect.width - (toggleRect.width + 12 + buttonsW), 18);
        var rightBtns = new Rect(headerRect.xMax - buttonsW, headerRect.y + 2, buttonsW, 18);

        bool fold = GetFold(part);
        EditorGUI.BeginChangeCheck();
        fold = EditorGUI.Foldout(toggleRect, fold, GUIContent.none, false);
        if (EditorGUI.EndChangeCheck()) { SetFold(part, fold); GUI.changed = true; }

        var title = $"{part.partName}   (HP:{part.maxHp:0.#}){(part.vital ? "  [Vital]" : "")}";
        EditorGUI.LabelField(labelRect, title, EditorStyles.boldLabel);

        var e = Event.current;
        if (e.type == EventType.MouseDown && e.button == 1 && headerRect.Contains(e.mousePosition))
        {
            if (!rightBtns.Contains(e.mousePosition) && !toggleRect.Contains(e.mousePosition))
            {
                fold = !fold; SetFold(part, fold); e.Use(); Repaint();
            }
        }

        using (new EditorGUI.DisabledScope(parent == null && indexInParent == 0))
        {
            if (GUI.Button(new Rect(rightBtns.x, rightBtns.y, 60, 18), "Up"))
                MoveInSiblings(siblingList, indexInParent, -1);
        }
        using (new EditorGUI.DisabledScope(parent == null && indexInParent == siblingList.Count - 1))
        {
            if (GUI.Button(new Rect(rightBtns.x + 62, rightBtns.y, 60, 18), "Down"))
                MoveInSiblings(siblingList, indexInParent, +1);
        }
        if (GUI.Button(new Rect(rightBtns.x + 124, rightBtns.y, 44, 18), "+Ch"))
            AddChild(part);
        if (GUI.Button(new Rect(rightBtns.x + 170, rightBtns.y, 44, 18), "Del"))
            ShowDeleteMenu(part, parent, siblingList, indexInParent);

        if (!GetFold(part)) return;

        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUI.indentLevel++;

            // --- 기본 속성 ---
            EditorGUI.BeginChangeCheck();
            var newName = EditorGUILayout.TextField("Name", part.partName);
            var newHp = EditorGUILayout.FloatField("Max HP", part.maxHp);
            var newVit = EditorGUILayout.Toggle("Vital", part.vital);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(part, "Edit Part");
                part.partName = newName; part.name = newName;
                part.maxHp = Mathf.Max(0, newHp);
                part.vital = newVit;
                EditorUtility.SetDirty(part);
            }

            // --- Parent 지정/변경 ---
            using (new EditorGUILayout.HorizontalScope())
            {
                var curParent = parent ?? FindParent(part, out _); // 안전: 루트 호출 시 parent == null
                var label = new GUIContent("Parent (same BodyDefinition)");
                EditorGUI.BeginChangeCheck();
                var picked = (BodyPartSO)EditorGUILayout.ObjectField(label, curParent, typeof(BodyPartSO), false);
                if (EditorGUI.EndChangeCheck())
                {
                    TryReparent(part, curParent, picked);
                }

                if (GUILayout.Button("Set None", GUILayout.Width(80)))
                {
                    if (curParent != null) TryReparent(part, curParent, null);
                }
            }

            // --- 자식 렌더링 ---
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Children", EditorStyles.boldLabel);

            if (part.children == null || part.children.Count == 0)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.HelpBox("자식 없음", MessageType.None);
                    if (GUILayout.Button("Add Child", GUILayout.Width(90)))
                        AddChild(part);
                }
            }
            else
            {
                for (int i = 0; i < part.children.Count; i++)
                {
                    var child = part.children[i];
                    if (child == null)
                    {
                        using (new EditorGUILayout.HorizontalScope("box"))
                        {
                            EditorGUILayout.HelpBox("NULL Child", MessageType.Warning);
                            if (GUILayout.Button("Remove", GUILayout.Width(70)))
                            {
                                Undo.RecordObject(part, "Remove Null Child");
                                part.children.RemoveAt(i--);
                                EditorUtility.SetDirty(part);
                            }
                        }
                        continue;
                    }

                    DrawPartRecursive(child, parent: part, siblingList: part.children, indexInParent: i);
                }
            }

            EditorGUI.indentLevel--;
        }
    }

    // ---------- Parent 지정/검증/리패런트 ----------

    void TryReparent(BodyPartSO node, BodyPartSO currentParent, BodyPartSO newParent)
    {
        if (newParent == currentParent) return;
        if (newParent == node)
        {
            EditorUtility.DisplayDialog("리패런트 불가", "자기 자신을 부모로 지정할 수 없습니다.", "OK");
            return;
        }
        if (newParent != null && IsDescendant(node, newParent))
        {
            EditorUtility.DisplayDialog("리패런트 불가", "자신의 자손을 부모로 지정할 수 없습니다.", "OK");
            return;
        }
        if (newParent != null && !IsSameOwner(newParent))
        {
            EditorUtility.DisplayDialog("다른 에셋", "같은 BodyDefinition의 서브에셋만 부모로 지정할 수 있습니다.", "OK");
            return;
        }

        Reparent(node, newParent);
        // 새 부모/루트 펼쳐주기
        if (newParent != null) { SetFold(newParent, true); }
        SetFold(node, true);
        Repaint();
    }

    bool IsSameOwner(BodyPartSO part)
    {
        if (part == null) return true;
        var a = AssetDatabase.GetAssetPath(part);
        var b = AssetDatabase.GetAssetPath(_def);
        return a == b;
    }

    void Reparent(BodyPartSO node, BodyPartSO newParent)
    {
        var oldParent = FindParent(node, out var oldList);

        Undo.RegisterCompleteObjectUndo(new Object[] { _def, oldParent ? (Object)oldParent : _def, newParent ? (Object)newParent : _def }, "Reparent Part");

        // 1) 기존 부모 목록(또는 루트)에서 제거
        if (oldParent == null) _def.roots.Remove(node);
        else oldList.Remove(node);

        // 2) 새 부모 목록(또는 루트)에 추가
        if (newParent == null)
        {
            _def.roots ??= new List<BodyPartSO>();
            _def.roots.Add(node);
        }
        else
        {
            newParent.children ??= new List<BodyPartSO>();
            newParent.children.Add(node);
            EditorUtility.SetDirty(newParent);
        }

        EditorUtility.SetDirty(_def);
        AssetDatabase.SaveAssets();
    }

    BodyPartSO FindParent(BodyPartSO node, out List<BodyPartSO> directList)
    {
        if (_def.roots != null && _def.roots.Contains(node))
        {
            directList = _def.roots; // 루트의 직접 리스트는 roots
            return null;
        }
        if (_def.roots != null)
        {
            foreach (var r in _def.roots)
            {
                var p = FindParentDFS(r, node, out directList);
                if (p != null || directList != null) return p;
            }
        }
        directList = null;
        return null;
    }

    BodyPartSO FindParentDFS(BodyPartSO cur, BodyPartSO target, out List<BodyPartSO> directList)
    {
        if (cur.children != null && cur.children.Contains(target))
        {
            directList = cur.children;
            return cur;
        }
        if (cur.children != null)
        {
            foreach (var c in cur.children)
            {
                var p = FindParentDFS(c, target, out directList);
                if (p != null || directList != null) return p;
            }
        }
        directList = null;
        return null;
    }

    bool IsDescendant(BodyPartSO ancestor, BodyPartSO maybeChild)
    {
        if (ancestor == null || maybeChild == null) return false;
        if (ancestor.children == null) return false;
        foreach (var c in ancestor.children)
        {
            if (c == maybeChild) return true;
            if (IsDescendant(c, maybeChild)) return true;
        }
        return false;
    }

    // ---------- 기존 조작 유틸 ----------

    void AddRoot()
    {
        Undo.RegisterCompleteObjectUndo(_def, "Add Root");
        var child = CreateBodyPartSubAsset(_def, "NewRoot");
        _def.roots ??= new List<BodyPartSO>();
        _def.roots.Add(child);
        EditorUtility.SetDirty(_def);
        AssetDatabase.SaveAssets();
        SetFold(child, true);
    }

    void AddChild(BodyPartSO parent)
    {
        Undo.RegisterCompleteObjectUndo(parent, "Add Child");
        var child = CreateBodyPartSubAsset(_def, "NewPart");
        parent.children ??= new List<BodyPartSO>();
        parent.children.Add(child);
        EditorUtility.SetDirty(parent);
        AssetDatabase.SaveAssets();
        SetFold(parent, true);
        SetFold(child, true);
    }

    void MoveInSiblings(List<BodyPartSO> list, int index, int delta)
    {
        if (list == null) return;
        int newIdx = Mathf.Clamp(index + delta, 0, list.Count - 1);
        if (newIdx == index) return;

        Undo.RegisterCompleteObjectUndo(_def, "Reorder Siblings");
        var item = list[index];
        list.RemoveAt(index);
        list.Insert(newIdx, item);
        EditorUtility.SetDirty(_def);
        AssetDatabase.SaveAssets();
    }

    void ShowDeleteMenu(BodyPartSO node, BodyPartSO parent, List<BodyPartSO> listRef, int index)
    {
        var menu = new GenericMenu();
        menu.AddItem(new GUIContent("Delete (Recursive)"), false, () => DeleteRecursive(node, parent, listRef, index));
        menu.AddItem(new GUIContent("Delete (Promote Children to Parent)"), false, () => DeletePromote(node, parent, listRef, index));
        menu.ShowAsContext();
    }

    void DeleteRecursive(BodyPartSO node, BodyPartSO parent, List<BodyPartSO> listRef, int index)
    {
        Undo.RegisterCompleteObjectUndo(new Object[] { _def, parent ? (Object)parent : _def }, "Delete Part (Recursive)");

        listRef.RemoveAt(index);
        DestroyTree(node);

        EditorUtility.SetDirty(_def);
        AssetDatabase.SaveAssets();
    }

    void DeletePromote(BodyPartSO node, BodyPartSO parent, List<BodyPartSO> listRef, int index)
    {
        Undo.RegisterCompleteObjectUndo(new Object[] { _def, parent ? (Object)parent : _def, node }, "Delete Part (Promote)");

        if (node.children != null && node.children.Count > 0)
        {
            foreach (var c in node.children)
                if (c != null) listRef.Insert(index, c);
        }

        if (parent == null) _def.roots.Remove(node);
        else listRef.Remove(node);

        Undo.DestroyObjectImmediate(node);
        EditorUtility.SetDirty(_def);
        AssetDatabase.SaveAssets();
    }

    void DestroyTree(BodyPartSO node)
    {
        if (node == null) return;
        if (node.children != null)
        {
            var copy = new List<BodyPartSO>(node.children);
            foreach (var c in copy) DestroyTree(c);
            node.children.Clear();
        }
        Undo.DestroyObjectImmediate(node);
    }

    static BodyPartSO CreateBodyPartSubAsset(BodyDefinitionSO owner, string name)
    {
        var part = ScriptableObject.CreateInstance<BodyPartSO>();
        part.name = name;
        part.partName = name;
        AssetDatabase.AddObjectToAsset(part, owner);
        EditorUtility.SetDirty(part);
        return part;
    }

    void VisitAll(System.Action<BodyPartSO> act)
    {
        if (_def.roots == null) return;
        foreach (var r in _def.roots) VisitDFS(r, act);
    }
    void VisitDFS(BodyPartSO n, System.Action<BodyPartSO> act)
    {
        if (n == null) return;
        act?.Invoke(n);
        if (n.children != null)
            foreach (var c in n.children)
                VisitDFS(c, act);
    }
}
