using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class StateManager : SingletonClass<StateManager>
{
    public enum eTreadState
    {
        None,
        SelectBox,
        ReadingBox
    }

    public eTreadState nowState=eTreadState.None;


}
