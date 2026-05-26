using System;
using System.Collections.Generic;

namespace IsoTilemap
{
    /// <summary>런타임 맵 모델에 기록하지 않는 타일 프레젠테이션(고스트·선택) 상태.</summary>
    internal sealed class TilePresentationStore
    {
        private readonly HashSet<Guid> _ghosted = new HashSet<Guid>();
        private readonly HashSet<Guid> _selected = new HashSet<Guid>();

        public bool IsGhosted(Guid tileId) => _ghosted.Contains(tileId);

        public bool IsSelected(Guid tileId) => _selected.Contains(tileId);

        public void SetGhosted(Guid tileId, bool ghosted)
        {
            if (ghosted)
                _ghosted.Add(tileId);
            else
                _ghosted.Remove(tileId);
        }

        public void SetSelected(Guid tileId, bool selected)
        {
            if (selected)
                _selected.Add(tileId);
            else
                _selected.Remove(tileId);
        }
    }
}
