using UnityEngine;

public abstract class MenuOption : MonoBehaviour {

    internal abstract void DoOption();

    internal abstract void Selected();

    internal abstract void Deselected();

}
