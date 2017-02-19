
using UnityEngine;
using UnityEngine.UI;

public class InstructionsUI : MonoBehaviour {

    public Text instructions;
    public Text tabText;

	// Use this for initialization
	void Start () {
        instructions.enabled = false;
        tabText.enabled = true;
	}
	
	// Update is called once per frame
	void Update () {

        if (Input.GetKeyDown(KeyCode.Tab)) {
            tabText.enabled = !tabText.enabled;
            instructions.enabled = !instructions.enabled;
        }

	}
}
