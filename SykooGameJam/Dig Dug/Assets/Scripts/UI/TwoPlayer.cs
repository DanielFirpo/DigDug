
using UnityEngine;

public class TwoPlayer : MenuOption {

    internal override void DoOption() {
        FindObjectOfType<GameManager>().NewGame(GameManager.GameMode.TwoPlayer);
        Debug.Log("Two Players!");
    }

    internal override void Selected() {
        transform.GetChild(0).gameObject.SetActive(true);//GetChild(0) is the selection icon
    }

    internal override void Deselected() {
        transform.GetChild(0).gameObject.SetActive(false);
    }
}