using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MyInput
{
    //클릭인풋
    public static bool Touch(TouchPhase phase,int index=0)
    {
#if UNITY_EDITOR

        switch (phase)
        {
            case TouchPhase.Began:
                    return Input.GetMouseButtonDown(0);

            case TouchPhase.Ended:
                    return Input.GetMouseButtonUp(0);

            case TouchPhase.Moved:
                    return Input.GetMouseButton(0);

            case TouchPhase.Stationary:
                    return Input.GetMouseButton(0);

            default:
                    return false;
        }


#else

        if (Input.GetTouch(index).phase == phase)
        {
            return true;
        }
        else return false;
#endif

    }

}
