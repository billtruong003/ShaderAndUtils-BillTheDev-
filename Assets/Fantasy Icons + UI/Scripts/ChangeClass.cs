using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChangeClass : MonoBehaviour
{
    [SerializeField] List<string> characterClass = new List<string>();
    [SerializeField] List<Sprite> characterImage = new List<Sprite>();
    private List<string> characterDescription = new List<string>();
    [SerializeField] List<GameObject> characterSkills = new List<GameObject>();

    [SerializeField] TextMeshProUGUI classTitle;
    [SerializeField] GameObject classImage;
    [SerializeField] TextMeshProUGUI classDescription;


    private int currentID;
    private int totalCharacters;


    // Start is called before the first frame update
    void Start()
    {
        characterDescription.Add("A fire mage is a character who uses magic to control and manipulate fire, causing high damage to enemies.\r\n\r\nThey have weaker defense compared to others.\r\n\r\nFire mages can cast spells and use abilities involving fire, such as summoning elementals, creating flames, and setting enemies on fire.");
        characterDescription.Add("A frost mage is a character who uses magic to control and manipulate ice and cold, freezing enemies and dealing moderate to high damage. \r\n\r\nThey have weaker defense compared to others. \r\n\r\nFrost mages can cast spells and use abilities involving ice and cold, such as summoning elementals, creating ice barriers, and freezing enemies.");
        characterDescription.Add("A cleric is a character who is able to heal and support allies through the use of magic. \r\n\r\nThey have a wide range of healing spells and abilities, and may also have access to some offensive spells and also have some skills for buffing and protecting allies.\r\n\r\nClerics have strong faith or religious ties, and their healing powers may be drawn from a deity or divine force.");
        characterDescription.Add("A necromancer is a character who is able to control and manipulate the dead through the use of magic. \r\n\r\nThey have the ability to raise and command the undead, using them as minions to fight for them. They also have access to spells and abilities that can drain life from their enemies and cause them to suffer from various negative effects. \r\n\r\nNecromancers are dark and sinister characters, delving into the forbidden arts of death magic.");
        characterDescription.Add("A poison mage is a character who uses magic to control and manipulate poison and toxic substances, poisoning enemies and causing them negative effects like damage over time, debuffs, or death. \r\n\r\nPoison mages can cast spells and use abilities involving poison and toxins, such as summoning elementals, creating poisonous clouds, and applying toxins to enemies. They are stealthy and ruthless.");

        characterDescription.Add("Arcane mages specialize in the manipulation of magic through the use of arcane spells and abilities. They are able to cast a wide variety of spells, often with a focus on offense or utility, and are able to harness the raw power of the arcane to achieve their goals. \r\n\r\nThey may have access to spells that deal damage, create illusions, or control the minds of others.");
        characterDescription.Add("An earth mage specializes in the manipulation of the earth and stone through the use of magic. They are able to cast spells and use abilities that involve the earth, such as summoning elementals made of stone, creating walls of rock, and causing earthquakes. \r\n\r\nThey may have access to spells that deal damage, as well as spells that provide protection and support for allies. \r\n\r\nThey are often played as tanks or melee characters, using their earth magic to defend themselves and their allies in combat.");
        characterDescription.Add("A druid able to manipulate the forces of nature through the use of magic. They have access to spells and abilities that involve nature and its elements, such as healing spells, spells that summon animals or plant life, and spells that control the weather. \r\n\r\nDruids are often played as a support role, using their healing spells to keep allies alive, and their spells that control the environment to help their allies and harm their enemies. \r\n");
        characterDescription.Add("A storm mage specializes in the manipulation of the weather and the forces of nature through the use of magic. They have access to spells and abilities that involve lightning, thunder, wind, and other aspects of the weather. \r\n\r\nThey are able to summon lightning bolts, create powerful gusts of wind, and cause massive storms that can damage enemies and disrupt their movements. \r\n\r\nIn addition to their offense spells, storm mages may also have access to spells that provide protection and support for allies, such as spells that create barriers of wind or call forth protective bolts of lightning.");
        characterDescription.Add("A water mage specializes in the manipulation of water and ice through the use of magic.\r\n\r\nThey have access to spells and abilities that involve water and ice, such as summoning elementals made of water, creating walls of ice, and freezing enemies. They may have access to spells that deal damage, as well as spells that provide protection and support for allies. \r\n\r\nThey may be played as tanks, melee characters, or support characters, using their water magic to defend themselves and their allies in combat and control the battlefield.\r\n");


        totalCharacters = characterClass.Count;
        currentID = 0;


        Debug.Log("Total characters: " + totalCharacters);


    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void NextCharacterClick() 
    {
        currentID ++;
        if (currentID > totalCharacters - 1) currentID = 0;

        for (int i = 0; i < totalCharacters; i++)
        {
            characterSkills[i].SetActive(false);
        }

        classTitle.text = characterClass[currentID];
        classDescription.text = characterDescription[currentID];
        classImage.GetComponent<Image>().sprite = characterImage[currentID];
        characterSkills[currentID].SetActive(true);

        Debug.Log("Current class: " + characterClass[currentID]);
    }
}
