

namespace Garunnir
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    public class UIUtility
    {
        public static void AdjustSize(Rect refMaxRange, RectTransform target, Texture texture)
        {
            AdjustSize(refMaxRange, target, texture, Vector2.one * 0.5f);
        }
        public static void AdjustSize(Rect refMaxRange, RectTransform target, Texture texture, Vector2 anchor)
        {
            target.sizeDelta = Vector2.zero;
            target.localPosition = Vector2.zero;
            float heightfactor = (float)texture.height / texture.width;
            float rectHeightFactor = (float)refMaxRange.height / refMaxRange.width;
            if (rectHeightFactor > heightfactor)
            {
                float adjust = target.rect.width * heightfactor;//height pixel
                var calY = adjust - target.rect.height;
                target.sizeDelta = new Vector2(0, calY);
                target.localPosition -= Vector3.up * calY * (anchor.y - 0.5f);
            }
            else
            {
                float adjust = target.rect.height / heightfactor;//height pixel
                var calX = adjust - target.rect.width;
                target.sizeDelta = new Vector2(calX, 0);
                target.localPosition -= Vector3.right * calX * (anchor.x - 0.5f);
            }
        }
    }

}
