using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class _playerInventory : MonoBehaviour
{
    public byte[] hotbar = new byte[9]; //hotbar with 9 slots, currently only supports blocks

    public int currentSlotIndex = 0; //start with slot 0 (0->8)

    public _gameIllustrator _illustrator; // assign illustrator and call its UI update function every time hotbar changes

    private void Start() {
        //hardcode some blocks in hotbar
        hotbar[0] = (byte)BlockType.Stone;
        hotbar[1] = (byte)BlockType.Dirt;
        hotbar[2] = (byte)BlockType.Grass;
        hotbar[3] = (byte)BlockType.Planks;
        hotbar[4] = (byte)BlockType.Bricks;
        hotbar[5] = (byte)BlockType.StoneBricks;
        hotbar[6] = (byte)BlockType.Sand;

        //update hotbar
        if (_illustrator != null) {
            _illustrator.UpdateHotbarUI();
        }
    }

    private void Update() {
        //change hotbar slot with number keys
        for(int i = 0; i < 9; i++) {
            if(Input.GetKeyDown(KeyCode.Alpha1 + i)) {
                currentSlotIndex = i;
                Debug.Log("changed slot to " + ((BlockType)hotbar[currentSlotIndex]).ToString());

                //update hotbar
                if (_illustrator != null) {
                    _illustrator.UpdateHotbarUI();
                }
            }
        }

        //also scroll through hotbar
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0) {
            currentSlotIndex--;
            if (currentSlotIndex < 0) currentSlotIndex = 8; // loop
            Debug.Log("changed slot to " + ((BlockType)hotbar[currentSlotIndex]).ToString());


            //update hotbar
            if (_illustrator != null) {
                _illustrator.UpdateHotbarUI();
            }

        } else if (scroll < 0) {
            currentSlotIndex++;
            if (currentSlotIndex > 8) currentSlotIndex = 0;// loop
            Debug.Log("changed slot to " + ((BlockType)hotbar[currentSlotIndex]).ToString());


            //update hotbar
            if (_illustrator != null) {
                _illustrator.UpdateHotbarUI();
            }

        }
    }

    //helper function to knmow what we have selected for placing
    public byte getSelectedBlockID() {
        return (byte)hotbar[currentSlotIndex];
    }
}
