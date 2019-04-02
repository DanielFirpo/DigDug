using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelSplitter : MonoBehaviour {

    [SerializeField]
    private Texture2D source;

    List<GameObject> splits;

    [SerializeField]
    private float size;
    [SerializeField]
    private float spacing;

    [SerializeField]
    private float xLoc;
    [SerializeField]
    private float yLoc;
    

    // Use this for initialization
    void Start() {

        splits = new List<GameObject>();

        for (int x = 0; x < 39; x++) {
            for (int y = 0; y < 39; y++) {
                Debug.Log("x " + x);
                Sprite newSprite = Sprite.Create(source, new Rect(x * size, y * size, size, size), new Vector2(0.5f, 0.5f));
                GameObject n = new GameObject();
                SpriteRenderer sr = n.AddComponent<SpriteRenderer>();
                sr.sprite = newSprite;
                n.transform.position = new Vector3(x * spacing - xLoc, y * spacing - yLoc, 1);
                n.transform.parent = this.transform;
                splits.Add(n);
                n.transform.localScale = new Vector3(1f, 1f, 1f);
                BoxCollider2D collider = n.AddComponent<BoxCollider2D>();
                collider.isTrigger = true;
            }
        }

    }

}
