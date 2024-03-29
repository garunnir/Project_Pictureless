#if !USE_OPENAI

using UnityEngine;
using UnityEditor;

namespace PixelCrushers.DialogueSystem.DialogueEditor
{

    /// <summary>
    /// Default DrawAI* method implementations when not using an AI content generation addon.
    /// These implementations draw nothing.
    /// </summary>
    public partial class DialogueEditorWindow
    {

        private bool DrawAIDatabaseFoldout(bool foldout)
        {
            return foldout;
        }

        public void OpenTranslateDatabasePanel()
        {
        }

        private void DrawAIGenerateConversationButton()
        {
        }

        public void DrawAIBranchingConversationButton(Conversation conversation)
        {
        }
        public void DrawAIBranchingMapButton(MapContainer mapContainer)
        {
        }
        private void DrawAIReviseTextButton(Asset asset, DialogueEntry entry, Field field)
        {
        }
        private void DrawAIReviseTextButton(Asset asset, Field field)
        {
        }
        private void DrawAIReviseTextButton(Asset asset, MapEntry entry, Field field)
        {
        }

        private void DrawAILocalizeTextButton(Asset asset, DialogueEntry entry, Field field)
        {
        }
        private void DrawAILocalizeTextButton(Asset asset, MapEntry entry, Field field)
        {
        }

        private void DrawAIPortraitSprites(Asset asset)
        {
        }

        public void DrawAIVoiceSelection(Actor actor)
        {
        }

        public void DrawAISequence(DialogueEntry entry, Field field)
        {
        }
        public void DrawAISequence(MapEntry entry, Field field)
        {
        }
    }
}
#endif