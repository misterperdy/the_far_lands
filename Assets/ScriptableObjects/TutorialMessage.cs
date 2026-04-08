using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Tutorial Message", menuName = "Tutorial/Message")]
public class TutorialMessage : ScriptableObject
{
    public string titleText;

    [TextArea(3,6)]
    public string contentText;

    public float displayDuration = 10f;
}
