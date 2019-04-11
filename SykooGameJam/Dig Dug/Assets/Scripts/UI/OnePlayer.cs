
using UnityEngine;

public class OnePlayer : MenuOption {

    internal override void DoOption() {
        Debug.Log("One Player!");
        FindObjectOfType<GameManager>().NewGame(GameManager.GameMode.OnePlayer);
    }

    internal override void Selected() {
        transform.GetChild(0).gameObject.SetActive(true);//GetChild(0) is the selection icon
    }

    internal override void Deselected() {
        transform.GetChild(0).gameObject.SetActive(false);
    }

}
