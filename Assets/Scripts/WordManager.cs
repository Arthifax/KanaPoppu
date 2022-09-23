using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.IO;
using System.Linq;
using UnityEditor;
using Random = UnityEngine.Random;
using MText;

public class WordManager : MonoBehaviour
{
    public GameObject bubbleSpawner;
    
    //public GameObject displayPrefab;
    public TextMeshPro wordDisplay;
    public Modular3DText wordDisplay3D;
    public TextMeshPro hitWordDisplay;
    public TextMeshPro scoreDisplay;

    //Strings containing all hiragana words and translations
    private string _hWordLine;
    private string _translationLine;

    //Array containing separated hiragana and romaji
    public List<string> _hWordList = new List<string>();
    public List<string> _translationList = new List<string>();

    //The word the player is creating from hitting the targets.
    private string correctWord;
    public List<char> correctWordSyllables = new List<char>();
    private char nextCorrectChar;
    private int currentCharCounter = 0;
    private int scoreCounter = 0;
    

    void Start()
    {
        
        //Get the TMP component on the displays
        wordDisplay = wordDisplay.GetComponentInChildren<TextMeshPro>();
        hitWordDisplay = hitWordDisplay.GetComponentInChildren<TextMeshPro>();
        scoreDisplay = scoreDisplay.GetComponentInChildren<TextMeshPro>();

        VocabFileToList();
        GenerateWord();
        bubbleSpawner.GetComponent<BubbleSpawner>().SpawnBubble();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            GenerateWord();
            Debug.Log("Skeet");
        }
    }

    void VocabFileToList()
    {
        TextAsset vocabData = (TextAsset)Resources.Load("VocabList");
        TextAsset vocabTranslationData = (TextAsset)Resources.Load("VocabListTranslation");
        
        //Reads the txt file and makes a List of separate lines
        _hWordLine = vocabData.text;
        _translationLine = vocabTranslationData.text;
        
        //Split the lines into separate words and place them in an array
        _hWordList = _hWordLine.Split(',').ToList();
        _translationList = _translationLine.Split(',').ToList();
    }

    private void GenerateWord()
    {
        int wordNum = Random.Range(0, _hWordList.Count);
        wordDisplay3D.Text = "Word: " + _translationList[wordNum];
        correctWord = _hWordList[wordNum];
        correctWordSyllables = correctWord.ToList();
        nextCorrectChar = correctWordSyllables[currentCharCounter];
    }

    private void killBubbles()
    {
        GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag("KanaBubble");
        foreach(GameObject kanaBubble in taggedObjects)
        {
            Destroy(kanaBubble);
        }
    }

    public void CheckWord(char hitSyllable)
    {
        if (hitSyllable == nextCorrectChar)
        {
            //Show the hit char on the display
            hitWordDisplay.text += hitSyllable;
            //Check to see if we've won the game
            if (hitWordDisplay.text == correctWord)
            {
                hitWordDisplay.text = "";
                currentCharCounter = 0;
                scoreCounter += 1;
                scoreDisplay.text = "Score: " + scoreCounter;
                GenerateWord();
                killBubbles();
                bubbleSpawner.GetComponent<BubbleSpawner>().SpawnBubble();
            }
            //If we haven't won the game move on to the next syllable
            else
            {
                currentCharCounter += 1;
                nextCorrectChar = correctWordSyllables[currentCharCounter];
            }
        }
        else
        {
            //If we guess wrong, You LOSE.
            Debug.Log("One mistake too many. You lose.");
            hitWordDisplay.text = "";
            currentCharCounter = 0;
            GenerateWord();
            killBubbles();
            bubbleSpawner.GetComponent<BubbleSpawner>().SpawnBubble();
        }
    }
}