using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using TMPro;
using Random = UnityEngine.Random;

public class BubbleSpawner : MonoBehaviour
{
    //Set up the prefab to spawn and the text inside
    public GameObject bubblePrefab;
    public GameObject wordManager;
    public TextMeshPro kanaDisplay;
    
    //List holding the bubbles to spawn
    public List<GameObject> bubbleSpawnlist = new List<GameObject>();

    //Strings containing all hiragana, romaji and ID
    private string _hSyllableLine;
    private string _romajiLine;
    private string _hiraganaID;

    //Array containing separated hiragana and romaji
    public string[] _hSyllableArray = new string[76];
    public string[] _romajiArray = new string[76];
    
    //Spawn Box Variables
    public Vector3 center;
    public Vector3 size;
    
    private List<char> syllablesToSpawn = new List<char>();
    

    void Start()
    {
        kanaDisplay = bubblePrefab.GetComponentInChildren<TextMeshPro>(); //Get the text inside of the bubble
        KanaFileToArrays(); //Fill arrays up with words from txt file
    }

    public void SpawnBubble()
    {
        //Spawn all bubble prefabs from List
        for (int i = 0; i < bubbleSpawnlist.Count; i++)
        {
            //Get random pos
            Vector3 pos = transform.localPosition + center + new Vector3(Random.Range(-size.x / 2, size.x / 2), Random.Range(-size.y / 2, size.y / 2) ,Random.Range(-size.z / 2, size.z / 2));
            
            //Set the Kana text to a hiragana
            if(i < wordManager.GetComponent<WordManager>().correctWordSyllables.Count)
            {
                kanaDisplay.text = wordManager.GetComponent<WordManager>().correctWordSyllables[i].ToString();
                Debug.Log(wordManager.GetComponent<WordManager>().correctWordSyllables[i]);
            }
            else
            {
                kanaDisplay.text = _hSyllableArray[Random.Range(0,_hSyllableArray.Length)];
            }

            //Spawn Bubble
            Instantiate(bubbleSpawnlist[i], pos, Quaternion.identity);
        }
    }
    
    private void KanaFileToArrays()
    {
        TextAsset kanaData = (TextAsset)Resources.Load("KanaChart");
        TextAsset kanaRomajiData = (TextAsset)Resources.Load("RomajiChart");

        //Move the first line into hiragana string, second line into romaji string
        _hSyllableLine = kanaData.text;
        _romajiLine = kanaRomajiData.text;
        
        //Split the lines into separate words and place them in an array
        _hSyllableArray = _hSyllableLine.Split(',');
        _romajiArray = _romajiLine.Split(',');
    }

    private void OnDrawGizmosSelected()
    {
        //Draws a red cube to indicate bubble spawn area in the viewport
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        Gizmos.DrawCube(transform.localPosition + center, size);
    }
}
