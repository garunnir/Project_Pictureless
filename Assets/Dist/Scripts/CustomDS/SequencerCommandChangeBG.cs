using UnityEngine;
using System.Collections;
using PixelCrushers.DialogueSystem;
using Garunnir;
using UnityEngine.UI;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{

    public class SequencerCommandChangeBG : SequencerCommand
    {
        string changeTo;
        public void Awake()
        {
            changeTo=GetParameterAs<string>(0,changeTo);
            Change();
            // Add your initialization code here. You can use the GetParameter***() and GetSubject()
            // functions to get information from the command's parameters. You can also use the
            // Sequencer property to access the SequencerCamera, CameraAngle, Speaker, Listener,
            // SubtitleEndTime, and other properties on the sequencer. If IsAudioMuted() is true, 
            // the player has muted audio.
            //
            // If your sequencer command only does something immediately and then finishes,
            // you can call Stop() here and remove the Update() method:
            //

            Stop();
            //
            // If you want to use a coroutine, use a Start() method in place of or in addition to
            // this method.
        }
        private void Change()
        {
            //이미지를 로드한다
            //
            RawImage img=GameManager.Instance.GetUIManager().GetBackground();
            img.texture=GameManager.Instance.imgDic[changeTo];
            //이미지 비율을 비교해서 적용한다.
            //원본 이미지 비율을 가져옴
            //지금 적용되어있는 랙트를 비교
            //원본이미지 폭에따라 길이를 재적용
            UIManager.AdjustSize(GameManager.Instance.GetUIManager().GetUpperRect(),img.rectTransform,img.texture);
            //float heightfactor = (float)img.texture.height / img.texture.width;
            //float adjust = img.rectTransform.rect.width * heightfactor;//height pixel
            //print(adjust - img.rectTransform.rect.height);
            //img.rectTransform.sizeDelta = new Vector2(0, adjust - img.rectTransform.rect.height);
            //모든 앵커에 적용될려면....


        }
        //public void Update()
        //{
        //    // Add any update code here. When the command is done, call Stop().
        //    // If you've called stop above in Awake(), you can delete this method.
        //}

        //public void OnDestroy()
        //{
        //    // Add your finalization code here. This is critical. If the sequence is cancelled and this
        //    // command is marked as "required", then only Awake() and OnDestroy() will be called.
        //    // Use it to clean up whatever needs cleaning at the end of the sequencer command.
        //    // If you don't need to do anything at the end, you can delete this method.
        //}

    }

}
