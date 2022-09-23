using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class BubbleScript : MonoBehaviour
{
    public int kanaIndexNumber;
    
    public void GetWordOnDeath()
    {
        string bubbleText = gameObject.GetComponentInChildren<TextMeshPro>().text;
        char hitSyllable = bubbleText[0];

        WordManager wordManager = GameObject.Find("WordManager").GetComponent<WordManager>();
        
        wordManager.CheckWord(hitSyllable);

        /*kanaIndexNumber = wordManager._hWordList.IndexOf(hitWord);
        Debug.Log(wordManager._hWordList.IndexOf(hitWord));
        
        wordManager.hitWordDisplay.text = wordManager._translationList[kanaIndexNumber];*/
    }
}
