using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using TMPro;
using System.Linq;
using System.Text;

public class Program : MonoBehaviour
{
    // The Data Container Blueprint matching your exact 25-comma layout configuration
    [SerializeField] private TMP_Text Set;

    public class MineralCard
    {
        public string Species1 { get; set; } = "";
        public string Species2 { get; set; } = "";
        public string Group1 { get; set; } = "";
        public string Group2 { get; set; } = "";
        public string Location1 { get; set; } = "";
        public string Location2 { get; set; } = "";
        public string Rarity { get; set; } = "";
        public string[] Colors { get; set; } = new string[4];
        public string Size { get; set; } = "";
        public string[] Elements { get; set; } = new string[9];
        public string[] Anions { get; set; } = new string[4];
        public int Price { get; set; }
        public int CardNumber;
    }
    public class GoalCard
    {
        public bool type { get; set; } = true;
        public string score { get; set; } = "";
        public List<int> amount { get; set; } = new List<int>();
    }
    public class ScoredSetEntry
    {
        public string SetType;
        public int AmountInSet;
        public List<int> PointProgression;
        public int FinalPoints;
        public List<MineralCard> cardsinset;
    }

    // Still your one and only master scoring tracker list!
    public static List<ScoredSetEntry> Scored = new List<ScoredSetEntry>();
    public static Dictionary<string, List<int>> WhiteList = new Dictionary<string, List<int>>();
    public static Dictionary<string, List<int>> BonusList = new Dictionary<string, List<int>>();
    public static List<string> UniqueColors = new List<string>();
    public static List<string> UniqueSpecies = new List<string>();
    public static List<string> Locations = new List<string>();
    public static List<string> Groups = new List<string>();
    public static List<string> Elements = new List<string>();


    public static List<int> two = new List<int> { 35 };
    public static List<int> three_four = new List<int> { 25, 40, 60 };
    public static List<int> five_nine = new List<int> { 15, 25, 40, 60, 90, 130, 180, 240 };
    public static List<int> ten_fourteen = new List<int> { 8, 16, 30, 46, 72, 102, 136, 176 };
    public static List<int> fifteen_nineteen = new List<int> { 6, 12, 24, 42, 66, 94, 126, 164 };
    public static List<int> twenty_twentyfour = new List<int> { 5, 10, 20, 35, 55, 80, 110, 150 };
    public static List<int> twentyfive_twentynine = new List<int> { 4, 8, 16, 28, 44, 64, 88, 116 };
    public static List<int> thirtyPlus = new List<int> { 3, 6, 12, 21, 33, 48, 58, 83 };
    public static List<int> differentColors = new List<int> { 2, 4, 8, 14, 22, 32, 44, 58 };
    public static List<int> differentLocsSpecies = new List<int> { 1, 2, 4, 7, 11, 16, 22, 29 };
    public static List<int> SizeBonus = new List<int> { 0, 0, 0, 20, 30, 50, 80, 100 };
    public static List<int> RarityBonus = new List<int> { 3, 6, 12, 21, 33, 48, 58, 183 };

    public static Dictionary<string, List<int>> DefaultValues = new Dictionary<string, List<int>>();
    // Master database lookup cabinet
    public static Dictionary<int, MineralCard> MasterDeck = new Dictionary<int, MineralCard>();
    public static Dictionary<int, GoalCard> MasterGoalDeck = new Dictionary<int, GoalCard>();
    public static bool IsDuplicationRuleEnabled = true;
    public static bool IsAdvancedModeEnabled = false;
    public static bool IsExponetialModeEnabled = false;
    public static bool IsMultiplicationModeEnabled = false;
    button uiButtonScript = null;
    private void Start()
    {
        uiButtonScript = GetComponent<button>();
    }
    public int Main()
    {

        if (uiButtonScript == null)
        {
            //   Debug.LogError("Fatal Error: Could not find the 'button' script on the ScoringManager object!");
            return 0;
        }


        // 1. Safety check: make sure the text file exists
        TextAsset deckAsset = Resources.Load<TextAsset>("card_strings");
        if (deckAsset == null)
        {
            // Debug.LogError("Fatal Error: Could not find card_strings.txt inside Assets/Resources/ folder!");
            return 0;
        }
        InitializeGameData();
        // 2. Read all lines off the disk and populate the master deck
        string[] rawTextLines = deckAsset.text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        LoadDatabaseCabinet(rawTextLines);

        AutomaticallyAssignSpeciesPoints();
        PreCountDeckAbundances();



        IsAdvancedModeEnabled = uiButtonScript.advancedMode;


        List<int> playerCardNumbers = uiButtonScript.cardsOwned;


        int leftoverMoney = uiButtonScript.leftoverCash;

        // 5. LOOK UP INSTANCES
        List<MineralCard> playerHand = FetchPlayerHandInstances(playerCardNumbers);

        TextAsset goalAsset = Resources.Load<TextAsset>("goal_strings");
        if (goalAsset == null)
        {
            //         Debug.LogError("Fatal Error: Could not find goal_strings.txt inside Assets/Resources/ folder!");
            return 0;
        }

        string[] TextLines = goalAsset.text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        LoadGoalDatabaseCabinet(TextLines);
        List<int> goalCardNumbers = new List<int>();

        if (!IsAdvancedModeEnabled)
        {
            goalCardNumbers = uiButtonScript.goalCards;
            LoadWhiteList(goalCardNumbers);
        }
        else
        {
            LoadWhiteList(Enumerable.Range(1, TextLines.Length).ToList());
        }

        // 6. EXECUTE SCORING CALCULATIONS
        int finalScore = CalculateYourScoringRules(playerHand, leftoverMoney, goalCardNumbers);


        StringBuilder sb = new StringBuilder();

        foreach (var entry in Scored.ToList())
        {
            if (entry.AmountInSet >= 2)
            {
                sb.AppendLine($"=== {entry.SetType} ===");
                sb.AppendLine($"Amount in Set: {entry.AmountInSet}");
                //     Debug.Log(entry.FinalPoints);
                sb.AppendLine($"Points: {entry.FinalPoints}");
                //  foreach (var card in entry.cardsinset)
                //{
                //    sb.AppendLine($"Id: {card.CardNumber}");
                //   }


                sb.AppendLine(); // blank line between entries
            }

        }
        Set.text = sb.ToString();

        return finalScore;

    }
    public static void InitializeGameData()
    {
        DefaultValues.Clear();
        DefaultValues["red"] = thirtyPlus;
        DefaultValues["orange"] = five_nine;
        DefaultValues["yellow"] = thirtyPlus;
        DefaultValues["green"] = thirtyPlus;
        DefaultValues["blue"] = thirtyPlus;
        DefaultValues["purple"] = twenty_twentyfour;
        DefaultValues["black"] = thirtyPlus;
        DefaultValues["brown"] = thirtyPlus;
        DefaultValues["colorless"] = twenty_twentyfour;
        DefaultValues["white"] = twentyfive_twentynine;
    }
    public static Dictionary<string, int> DeckGroupCounts = new Dictionary<string, int>();
    public static Dictionary<string, int> DeckElementCounts = new Dictionary<string, int>();
    public static Dictionary<string, int> DeckLocationCounts = new Dictionary<string, int>();


    public static void PreCountDeckAbundances()
    {
        DeckGroupCounts.Clear();
        DeckElementCounts.Clear();
        DeckLocationCounts.Clear();

        foreach (var card in MasterDeck.Values)
        {
            // 1. Tally up Group frequencies in the entire deck
            if (!string.IsNullOrEmpty(card.Group1))
            {
                if (!DeckGroupCounts.ContainsKey(card.Group1)) DeckGroupCounts[card.Group1] = 0;
                DeckGroupCounts[card.Group1]++;
            }
            if (!string.IsNullOrEmpty(card.Group2))
            {
                if (!DeckGroupCounts.ContainsKey(card.Group2)) DeckGroupCounts[card.Group2] = 0;
                DeckGroupCounts[card.Group2]++;
            }
            if (!string.IsNullOrEmpty(card.Location2))
            {
                if (!DeckLocationCounts.ContainsKey(card.Location2)) DeckLocationCounts[card.Location2] = 0;
                DeckLocationCounts[card.Location2]++;
            }

            // 2. Tally up individual Element frequencies in the entire deck
            foreach (var elem in card.Elements)
            {
                if (!string.IsNullOrEmpty(elem))
                {
                    if (!DeckElementCounts.ContainsKey(elem)) DeckElementCounts[elem] = 0;
                    DeckElementCounts[elem]++;
                }
            }
        }
    }
    public static void AutomaticallyAssignSpeciesPoints()
    {
        // Step 1: Create a temporary checklist to count how many times each species exists in the deck
        Dictionary<string, int> deckDistributionCount = new Dictionary<string, int>();

        foreach (var card in MasterDeck.Values)
        {
            // Count Species 1 appearances
            if (!string.IsNullOrEmpty(card.Species1))
            {
                if (!deckDistributionCount.ContainsKey(card.Species1)) deckDistributionCount[card.Species1] = 0;
                deckDistributionCount[card.Species1]++;
            }
            // Count Species 2 appearances (Emulates your Excel columns B & C check!)
            if (!string.IsNullOrEmpty(card.Species2))
            {
                if (!deckDistributionCount.ContainsKey(card.Species2)) deckDistributionCount[card.Species2] = 0;
                deckDistributionCount[card.Species2]++;
            }
        }

        // Step 2: Look at the final counts and assign your point lists matching your scoring matrix!
        foreach (var entry in deckDistributionCount)
        {
            string speciesName = entry.Key;
            int totalCardsInDeck = entry.Value; // This is exactly what your COUNTIF formula calculated!

            // Map the distribution number straight to your balancing lists from your scoring chart
            if (totalCardsInDeck == 2)
                DefaultValues[speciesName] = two;
            else if (totalCardsInDeck >= 3 && totalCardsInDeck <= 4)
                DefaultValues[speciesName] = three_four;
            else if (totalCardsInDeck >= 5 && totalCardsInDeck <= 9)
                DefaultValues[speciesName] = five_nine;
            else if (totalCardsInDeck >= 10 && totalCardsInDeck <= 14)
                DefaultValues[speciesName] = ten_fourteen;
            else if (totalCardsInDeck >= 15 && totalCardsInDeck <= 19)
                DefaultValues[speciesName] = fifteen_nineteen;
            else if (totalCardsInDeck >= 20 && totalCardsInDeck <= 24)
                DefaultValues[speciesName] = twenty_twentyfour;
            else if (totalCardsInDeck >= 25 && totalCardsInDeck <= 29)
                DefaultValues[speciesName] = twentyfive_twentynine;
            else
                DefaultValues[speciesName] = thirtyPlus;
        }

        //  Console.WriteLine($"[System] Excel formula replicated! Automatically mapped balanced matrices onto {DefaultValues.Count} unique species slots.");
    }

    public void AutoAssignGoalCardPointProgressions()
    {
        // Make sure your deck frequency metrics are calculated first!
        PreCountDeckAbundances();

        foreach (var goalEntry in MasterGoalDeck.Values)
        {
            // 🌟 RULE: Only auto-assign for Normal type goals that left their Amount blank!
            if (goalEntry.type == true && goalEntry.amount.Count == 0)
            {
                string keyword = goalEntry.score; // e.g., "CalciteGroup", "Cu", "Michigan", "Silicates"
                int deckAbundanceCount = 0;

                // 1. DYNAMIC DECK FREQUENCY LOOKUP: Scan your pre-counted dictionaries

                string lookupKeyword = keyword;
                if (keyword.EndsWith("Group"))
                {
                    lookupKeyword = keyword.Substring(0, keyword.Length - "Group".Length);
                }
                if (DeckGroupCounts.ContainsKey(lookupKeyword))
                {
                    deckAbundanceCount = DeckGroupCounts[lookupKeyword];
                }
                else if (DeckElementCounts.ContainsKey(lookupKeyword))
                {
                    deckAbundanceCount = DeckElementCounts[lookupKeyword];
                }
                else if (DeckLocationCounts.ContainsKey(lookupKeyword))
                {
                    deckAbundanceCount = DeckLocationCounts[lookupKeyword];
                }

                // 2. MATRIX MAPPING: Check the frequency size and attach the correct balanced points list!
                if (deckAbundanceCount == 2)
                    goalEntry.amount = two;
                else if (deckAbundanceCount >= 3 && deckAbundanceCount <= 4)
                    goalEntry.amount = three_four;
                else if (deckAbundanceCount >= 5 && deckAbundanceCount <= 9)
                    goalEntry.amount = five_nine;
                else if (deckAbundanceCount >= 10 && deckAbundanceCount <= 14)
                    goalEntry.amount = ten_fourteen;
                else if (deckAbundanceCount >= 15 && deckAbundanceCount <= 19)
                    goalEntry.amount = fifteen_nineteen;
                else if (deckAbundanceCount >= 20 && deckAbundanceCount <= 24)
                    goalEntry.amount = twenty_twentyfour;
                else if (deckAbundanceCount >= 25 && deckAbundanceCount <= 29)
                    goalEntry.amount = twentyfive_twentynine;
                else
                {
                    // Fallback: If it's a general broad macro goal (like "DifSpecies", "USA", "Multicolored")
                    // it won't be found in the deck frequency maps. Give it the standard thirtyPlus chart!
                    goalEntry.amount = thirtyPlus;
                }
            }
        }

        // Debug.Log("[Goals System] Replicated Excel logic! Dynamically mapped point curves across the Goal Deck.");
    }
    public static void LoadGoalDatabaseCabinet(string[] entries)
    {
        MasterGoalDeck.Clear();

        foreach (string entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry)) continue;

            // Splits the row by commas (Layout is always exactly 4 fields: 0 to 3)
            string[] fields = entry.Split(',');
            if (fields.Length < 3) continue; // Safety check remains perfectly safe!

            int.TryParse(fields[0].Trim(), out int goalId);

            GoalCard goal = new GoalCard();
            string goalTypeString = fields[1].Trim().ToLower();
            goal.type = (goalTypeString == "normal");
            goal.score = fields[2].Trim();

            string rawAmountData = fields.Length > 3 ? fields[3].Trim() : "";

            // ==========================================================
            // SMART PARSER: DETECT SEMICOLON PROGRESSION LISTS FIRST
            // This instantly catches Goals 41, 42, and 53 from your text sheet!
            // ==========================================================
            if (rawAmountData.Contains(";"))
            {
                string[] pointTokens = rawAmountData.Split(';');
                foreach (string token in pointTokens)
                {
                    if (int.TryParse(token.Trim(), out int parsedPoint))
                    {
                        goal.amount.Add(parsedPoint);
                    }
                }
            }
            // ==========================================================
            // FALLBACK A: IT'S A NORMAL GOAL (Amount is blank "")
            // ==========================================================
            else if (goal.type == true)
            {
                goal.amount = new List<int>(); // Empty list so Count == 0 works perfectly
            }

            else
            {
                if (int.TryParse(rawAmountData, out int singleValue))
                {
                    goal.amount = new List<int> { singleValue }; // Stores flat number at position 0
                }
                else
                {
                    goal.amount = new List<int> { 0 }; // Safe default fallback if box is completely empty
                }
            }

            MasterGoalDeck.Add(goalId, goal);
        }
        //      Debug.Log($"[Goals Engine] Dynamic mapping complete. Mapped {MasterGoalDeck.Count} rules slots safely.");
    }
    public static void LoadDatabaseCabinet(string[] entries)
    {
        int rowCounter = 1;
        foreach (string entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry)) continue;

            string[] fields = entry.Split(',');
            MineralCard card = new MineralCard();

            // Core data columns (Indices 0 to 6)
            card.Species1 = fields[0];
            card.Species2 = fields[1];
            card.Group1 = fields[2];
            card.Group2 = fields[3];
            card.Location1 = fields[4]; // 🌟 Maps "California" straight into Location1
            card.Location2 = fields[5]; // 🌟 Maps "USA" straight into Location2
            card.Rarity = fields[6];

            // 4 Colors: Shifted from indices 6-9 to indices 7-10
            card.Colors[0] = fields[7];
            card.Colors[1] = fields[8];
            card.Colors[2] = fields[9];
            card.Colors[3] = fields[10];
            card.Colors = card.Colors.Where(c => !string.IsNullOrEmpty(c)).Distinct().ToArray();

            // Size: Shifted from index 10 to 11
            card.Size = fields[11];

            // 9 Elements: Shifted to start at index 12 instead of 11 (Loop checks 12 to 20)
            for (int i = 0; i < 9; i++)
            {
                card.Elements[i] = fields[12 + i];
            }

            // 4 Anions: Shifted from indices 20-23 to indices 21-24
            card.Anions[0] = fields[21];
            card.Anions[1] = fields[22];
            card.Anions[2] = fields[23];
            card.Anions[3] = fields[24];

            card.Elements = card.Elements.Where(e => !string.IsNullOrEmpty(e)).Distinct().ToArray();

            card.Anions = card.Anions.Where(a => !string.IsNullOrEmpty(a)).Distinct().ToArray();
            // Price: Shifted from index 24 to the final index 25 slot
            int.TryParse(fields[25], out int targetPrice);
            card.Price = targetPrice;
            card.CardNumber = rowCounter;

            // File it safely away using the unique rowCounter tracking number as the dictionary key!
            MasterDeck.Add(rowCounter, card);
            rowCounter++;
        }
    }

    public static List<int> ParseUserNumbers(string input)
    {
        List<int> numbers = new List<int>();
        // Split input text by spaces, commas, or semicolons
        string[] tokens = input.Split(new char[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string token in tokens)
        {
            if (int.TryParse(token, out int num))
            {
                numbers.Add(num);
            }
        }
        return numbers;
    }

    public static List<MineralCard> FetchPlayerHandInstances(List<int> typedKeys)
    {
        List<MineralCard> outputHand = new List<MineralCard>();
        Console.WriteLine("\n--- Processing Player Hand ---");
        foreach (int key in typedKeys)
        {
            if (MasterDeck.ContainsKey(key))
            {
                MineralCard instance = MasterDeck[key];
                //   Console.WriteLine($"Loaded: Card #{key} -> {instance.Species1} ({instance.Location2})");
                outputHand.Add(instance);
            }
            else
            {
                //   Console.WriteLine($"[Warning] Card #{key} does not exist in the database!");
            }
        }
        return outputHand;
    }
    public static (Dictionary<string, List<int>>, Dictionary<string, List<int>>) LoadWhiteList(List<int> goalCardNumbers)
    {
        foreach (var card in goalCardNumbers)
        {
            var goalCard = MasterGoalDeck[card];
            if (goalCard.type)
            {
                WhiteList[goalCard.score] = goalCard.amount;
            }
            else if (!IsAdvancedModeEnabled)
            {
                BonusList[goalCard.score] = goalCard.amount;
            }
        }
        return (WhiteList, BonusList);
    }
    public static void CalculateScore(ScoredSetEntry setEntry, List<int> goalCardNumbers)
    {
        int lookupIndex = setEntry.AmountInSet - 2;

        // 1. Check if the index is valid (A set of 1 or fewer cards is not a set!)
        if (lookupIndex >= 0)
        {
            if (lookupIndex < setEntry.PointProgression.Count)
            {
                setEntry.FinalPoints = setEntry.PointProgression[lookupIndex];
            }
            else
            {
                if (setEntry.PointProgression.Count == 0)
                {
                    //      Debug.LogError($"PointProgression empty for {setEntry.SetType}");
                    setEntry.FinalPoints = 0;
                    return;
                }
                setEntry.FinalPoints = setEntry.PointProgression[setEntry.PointProgression.Count - 1];
            }
        }
        else
        {
            //      Debug.LogError("set of 1 or fewer");
            setEntry.FinalPoints = 0;
        }

        if (BonusList.ContainsKey("DoubleRarity"))
        {
            if (setEntry.SetType == "Rarity")
            {
                setEntry.FinalPoints *= 2;
            }
        }

        foreach (var item in Scored.ToList())
        {
            // 1. Safety Checks
            if (ReferenceEquals(item, setEntry)) continue;
            if (item.cardsinset.Count == 0 || setEntry.cardsinset.Count == 0) continue;
            if (item.cardsinset.Count != setEntry.cardsinset.Count) continue; // Quick escape if counts differ

            // 2. Robust Sequence Comparison (Independent of order)
            var cards1 = item.cardsinset.Select(c => c.CardNumber).OrderBy(n => n);
            var cards2 = setEntry.cardsinset.Select(c => c.CardNumber).OrderBy(n => n);

            bool isExactSameCards = cards1.SequenceEqual(cards2);

            if (isExactSameCards && IsDuplicationRuleEnabled)
            {
                //     Debug.LogError($"Found dupe between {item.SetType} and {setEntry.SetType}");

                if (setEntry.FinalPoints > item.FinalPoints)
                {
                    // Find the actual mutable reference inside the original Scored collection
                    var originalItem = Scored.First(entry => entry.SetType == item.SetType);
                    originalItem.FinalPoints = 0;
                }
                else
                {
                    setEntry.FinalPoints = 0;
                    break;
                }
            }
        }
        return;
    }
    public static ScoredSetEntry MakeSetEntry(string settype, List<int> points, MineralCard card)
    {
        var entry = new ScoredSetEntry
        {
            SetType = settype,
            AmountInSet = 1,
            PointProgression = points,
            FinalPoints = 0,
            cardsinset = new List<MineralCard> { card }
        };
        return entry;
    }
    // ====================================================================
    // YOUR WORKSPACE: WRITE YOUR SET-COLLECTION AND SCORING ALGORITHMS HERE
    // ====================================================================
    public static int CalculateYourScoringRules(List<MineralCard> hand, int leftoverMoney, List<int> goalCardNumbers)
    {
        int score = 0;
        Locations.Clear();
        Elements.Clear();
        Groups.Clear();
        Scored.Clear();


        // Loop through the hand using standard properties to run your math!
        foreach (var card in hand)
        {
            if (!UniqueSpecies.Contains(card.Species1))
            {
                UniqueSpecies.Add(card.Species1);
            }
            if (!UniqueSpecies.Contains(card.Species2))
            {
                UniqueSpecies.Add(card.Species2);
            }
            //   if (!UniqueLocations.Contains(card.Location1))
            // {
            Locations.Add(card.Location1);
            //}

            Groups.Add(card.Group1);
            Groups.Add(card.Group2);
            foreach (var element in card.Elements)
            {
                Elements.Add(element);
            }
            foreach (var color in card.Colors)
            {
                if (!string.IsNullOrWhiteSpace(color) && !UniqueColors.Contains(color))
                {
                    UniqueColors.Add(color);
                }
            }
            if (BonusList.ContainsKey("Abundance5to9"))
            {
                int existingIndex = Scored.FindIndex(entry => entry.SetType == "5-9 Groups");

                if (existingIndex == -1)
                {
                    // First time seeing it? Add a brand new ledger entry row!
                    Scored.Add(MakeSetEntry("5-9 Groups", thirtyPlus, card));
                }
                else
                {
                    var existingEntry = Scored[existingIndex];
                    existingEntry.AmountInSet++;
                    existingEntry.cardsinset.Add(card);

                    Scored[existingIndex] = existingEntry;
                }
            }
            if (BonusList.ContainsKey("Abundance2to4"))
            {
                int existingIndex = Scored.FindIndex(entry => entry.SetType == "2-4 Groups");

                if (existingIndex == -1)
                {
                    // First time seeing it? Add a brand new ledger entry row!
                    Scored.Add(MakeSetEntry("2-4 Groups", thirtyPlus, card));
                }
                else
                {
                    var existingEntry = Scored[existingIndex];
                    existingEntry.AmountInSet++;
                    existingEntry.cardsinset.Add(card);

                    Scored[existingIndex] = existingEntry;
                }
            }
            if (BonusList.ContainsKey("LocAbund5to9"))
            {
                int existingIndex = Scored.FindIndex(entry => entry.SetType == "5-9 Locations");

                if (existingIndex == -1)
                {
                    // First time seeing it? Add a brand new ledger entry row!
                    Scored.Add(MakeSetEntry("5-9 Locations", thirtyPlus, card));
                }
                else
                {
                    var existingEntry = Scored[existingIndex];
                    existingEntry.AmountInSet++;
                    existingEntry.cardsinset.Add(card);

                    Scored[existingIndex] = existingEntry;
                }
            }
            if (BonusList.ContainsKey("LocAbund3to4"))
            {
                int existingIndex = Scored.FindIndex(entry => entry.SetType == "3-4 Locations");

                if (existingIndex == -1)
                {
                    // First time seeing it? Add a brand new ledger entry row!
                    Scored.Add(MakeSetEntry("3-4 Locations", thirtyPlus, card));
                }
                else
                {
                    var existingEntry = Scored[existingIndex];
                    existingEntry.AmountInSet++;
                    existingEntry.cardsinset.Add(card);

                    Scored[existingIndex] = existingEntry;
                }
            }
            if (BonusList.ContainsKey("LocAbund2"))
            {
                int existingIndex = Scored.FindIndex(entry => entry.SetType == "2 Locations");

                if (existingIndex == -1)
                {
                    // First time seeing it? Add a brand new ledger entry row!
                    Scored.Add(MakeSetEntry("2 Locations", thirtyPlus, card));
                }
                else
                {
                    var existingEntry = Scored[existingIndex];
                    existingEntry.AmountInSet++;
                    existingEntry.cardsinset.Add(card);

                    Scored[existingIndex] = existingEntry;
                }
            }
            if (BonusList.ContainsKey("ElemAbund2to4"))
            {
                int existingIndex = Scored.FindIndex(entry => entry.SetType == "2-4 Elements");

                if (existingIndex == -1)
                {
                    Scored.Add(MakeSetEntry("2-4 Elements", thirtyPlus, card));
                }
                else
                {
                    var existingEntry = Scored[existingIndex];
                    existingEntry.AmountInSet++;
                    existingEntry.cardsinset.Add(card);

                    Scored[existingIndex] = existingEntry;
                }
            }
            if (BonusList.ContainsKey("ElemAbund5to9"))
            {
                int existingIndex = Scored.FindIndex(entry => entry.SetType == "5-9 Elements");

                if (existingIndex == -1)
                {
                    // First time seeing it? Add a brand new ledger entry row!
                    Scored.Add(MakeSetEntry("5-9 Elements", thirtyPlus, card));
                }
                else
                {
                    var existingEntry = Scored[existingIndex];
                    existingEntry.AmountInSet++;
                    existingEntry.cardsinset.Add(card);

                    Scored[existingIndex] = existingEntry;
                }
            }

            List<string> WhitelistedAttributes = new List<string> { card.Species1, card.Species2, card.Rarity, card.Size };

            List<string> otherAttributes = new List<string>
            {
                card.Group1, card.Group2, card.Location1
            };

            WhitelistedAttributes.AddRange(card.Colors);
            otherAttributes.AddRange(card.Elements);
            otherAttributes.AddRange(card.Anions);


            foreach (var attribute in otherAttributes)
            {
                // Skip completely blank columns immediately
                if (string.IsNullOrEmpty(attribute)) continue;

                if (WhiteList.ContainsKey(attribute))
                {
                    // 1. Look up the index position inside your single tracking ledger list
                    int existingIndex = Scored.FindIndex(entry => entry.SetType == attribute);

                    if (existingIndex == -1)
                    {
                        // First time seeing it? Add a brand new ledger entry row!
                        Scored.Add(MakeSetEntry(attribute, WhiteList.ContainsKey(attribute) ? WhiteList[attribute] : new List<int>(), card));
                    }
                    else
                    {
                        // Seen it before? Pull the structural data copy out by its index position
                        var existingEntry = Scored[existingIndex];

                        // Modify your temporary workspace parameters safely
                        existingEntry.AmountInSet++;
                        existingEntry.cardsinset.Add(card);

                        // SAVE IT BACK: Overwrite the list slot with your updated copy!
                        Scored[existingIndex] = existingEntry;
                    }
                }
            }
            foreach (var attribute in WhitelistedAttributes)
            {
                if (string.IsNullOrEmpty(attribute)) continue;

                int existingIndex = 0;
                if (attribute == "C" || attribute == "U")
                {
                    continue;
                }
                if (attribute == "R" || attribute == "S")
                {
                    existingIndex = Scored.FindIndex(entry => entry.SetType == "Rarity");
                    if (existingIndex == -1)
                    {
                        Scored.Add(MakeSetEntry("Rarity", RarityBonus, card));
                    }
                    else
                    {
                        // Seen it before? Pull the structural data copy out by its index position
                        var existingEntry = Scored[existingIndex];

                        // Modify your temporary workspace parameters safely
                        existingEntry.AmountInSet++;
                        existingEntry.cardsinset.Add(card);

                        // SAVE IT BACK: Overwrite the list slot with your updated copy!
                        Scored[existingIndex] = existingEntry;
                    }
                    continue;
                }
                else
                {
                    // 1. Look up the index position inside your single tracking ledger list
                    existingIndex = Scored.FindIndex(entry => entry.SetType == attribute);
                }


                if (existingIndex == -1)
                {
                    // First time seeing it? Add a brand new ledger entry row!
                    List<int> points;
                    if (attribute == "TN" || attribute == "cabinet" || attribute == "mini")
                    {
                        points = SizeBonus;
                    }
                    else
                    {
                        points = DefaultValues.ContainsKey(attribute) ? DefaultValues[attribute] : new List<int>();
                    }
                    Scored.Add(MakeSetEntry(attribute, points, card));
                }
                else
                {
                    // Seen it before? Pull the structural data copy out by its index position
                    var existingEntry = Scored[existingIndex];

                    // Modify your temporary workspace parameters safely
                    existingEntry.AmountInSet++;
                    existingEntry.cardsinset.Add(card);

                    // SAVE IT BACK: Overwrite the list slot with your updated copy!
                    Scored[existingIndex] = existingEntry;
                }
            }




            score += card.Price;
        }
        if (BonusList.ContainsKey("DifColors"))
        {
            Scored.Add(new ScoredSetEntry
            {
                SetType = "Different Colors",
                AmountInSet = UniqueColors.Count,
                PointProgression = differentColors,
                FinalPoints = 0,
                cardsinset = new List<MineralCard>()
            });
        }
        if (BonusList.ContainsKey("DifSpecies"))
        {
            Scored.Add(new ScoredSetEntry
            {
                SetType = "Different Species",
                AmountInSet = UniqueSpecies.Count,
                PointProgression = differentLocsSpecies,
                FinalPoints = 0,
                cardsinset = new List<MineralCard>()
            });
        }
        List<string> uniquelocs = new HashSet<string>(Locations).ToList();
        if (BonusList.ContainsKey("DifLocations"))
        {
            Scored.Add(new ScoredSetEntry
            {
                SetType = "Different Locations",
                AmountInSet = uniquelocs.Count,
                PointProgression = differentLocsSpecies,
                FinalPoints = 0,
                cardsinset = new List<MineralCard>()
            });
        }
        if (BonusList.ContainsKey("Under5k"))
        {
            var amount = 0;
            foreach (var card in hand)
            {
                if (card.Price < 5)
                {
                    amount++;
                }
            }
            Scored.Add(new ScoredSetEntry
            {
                SetType = "Under5k",
                AmountInSet = amount,
                PointProgression = BonusList["Under5k"],
                FinalPoints = 0,
                cardsinset = new List<MineralCard>()
            });
        }
        if (BonusList.ContainsKey("Over20k"))
        {
            var amount = 0;
            foreach (var card in hand)
            {
                if (card.Price > 20)
                {
                    amount++;
                }
            }
            Scored.Add(new ScoredSetEntry
            {
                SetType = "Over20k",
                AmountInSet = amount,
                PointProgression = BonusList["Over20k"],
                FinalPoints = 0,
                cardsinset = new List<MineralCard>()
            });
        }
        if (BonusList.ContainsKey("TwoSpecies"))
        {
            var amount = 0;
            foreach (var card in hand)
            {
                if (card.Species2 != "")
                {
                    amount++;
                }
            }
            Scored.Add(new ScoredSetEntry
            {
                SetType = "TwoSpecies",
                AmountInSet = amount,
                PointProgression = BonusList["TwoSpecies"],
                FinalPoints = 0,
                cardsinset = new List<MineralCard>()
            });
        }
        if (BonusList.ContainsKey("NoThumbnails"))
        {
            var scorer = true;
            foreach (var card in hand)
            {
                if (card.Size == "TN")
                {
                    scorer = false;
                }
            }
            if (scorer)
            {
                Scored.Add(new ScoredSetEntry
                {
                    SetType = "NoThumbnails",
                    AmountInSet = 2,
                    PointProgression = BonusList["NoThumbnails"],
                    FinalPoints = 0,
                    cardsinset = new List<MineralCard>()
                });
            }
        }
        if (BonusList.ContainsKey("NoMiniatures"))
        {
            var scorer = true;
            foreach (var card in hand)
            {
                if (card.Size == "mini")
                {
                    scorer = false;
                }
            }
            if (scorer)
            {
                Scored.Add(new ScoredSetEntry
                {
                    SetType = "NoMiniatures",
                    AmountInSet = 2,
                    PointProgression = BonusList["NoMiniatures"],
                    FinalPoints = 0,
                    cardsinset = new List<MineralCard>()
                });
            }
        }
        if (BonusList.ContainsKey("NoCabinets"))
        {
            var scorer = true;
            foreach (var card in hand)
            {
                if (card.Size == "cabinet")
                {
                    scorer = false;
                }
            }
            if (scorer)
            {
                Scored.Add(new ScoredSetEntry
                {
                    SetType = "NoCabinets",
                    AmountInSet = 2,
                    PointProgression = BonusList["NoCabinets"],
                    FinalPoints = 0,
                    cardsinset = new List<MineralCard>()
                });
            }
        }
        if (BonusList.ContainsKey("NoRareSpecial"))
        {
            var scorer = true;
            foreach (var card in hand)
            {
                if (card.Rarity == "R" || card.Rarity == "S")
                {
                    scorer = false;
                }
            }
            if (scorer)
            {
                Scored.Add(new ScoredSetEntry
                {
                    SetType = "NoRareSpecial",
                    AmountInSet = 2,
                    PointProgression = BonusList["NoRareSpecial"],
                    FinalPoints = 0,
                    cardsinset = new List<MineralCard>()
                });
            }
        }
        if (BonusList.ContainsKey("NoCommon"))
        {
            var scorer = true;
            foreach (var card in hand)
            {
                if (card.Rarity == "C")
                {
                    scorer = false;
                }
            }
            if (scorer)
            {
                Scored.Add(new ScoredSetEntry
                {
                    SetType = "NoCommon",
                    AmountInSet = 2,
                    PointProgression = BonusList["NoCommon"],
                    FinalPoints = 0,
                    cardsinset = new List<MineralCard>()
                });
            }
        }
        if (BonusList.ContainsKey("NoSilicates"))
        {
            var scorer = true;
            foreach (var card in hand)
            {
                foreach (var elem in card.Anions)
                {
                    if (elem == "Silicates")
                    {
                        scorer = false;
                    }
                }
            }
            if (scorer)
            {
                Scored.Add(new ScoredSetEntry
                {
                    SetType = "NoSilicates",
                    AmountInSet = 2,
                    PointProgression = BonusList["NoSilicates"],
                    FinalPoints = 0,
                    cardsinset = new List<MineralCard>()
                });
            }
        }
        if (BonusList.ContainsKey("NoMexico"))
        {
            var scorer = true;
            foreach (var card in hand)
            {
                if (card.Location2 == "Mexico")
                {
                    scorer = false;
                }
            }
            if (scorer)
            {
                Scored.Add(new ScoredSetEntry
                {
                    SetType = "NoMexico",
                    AmountInSet = 2,
                    PointProgression = BonusList["NoMexico"],
                    FinalPoints = 0,
                    cardsinset = new List<MineralCard>()
                });
            }
        }
        if (BonusList.ContainsKey("NoCanada"))
        {
            var scorer = true;
            foreach (var card in hand)
            {
                if (card.Location2 == "Canada")
                {
                    scorer = false;
                }
            }
            if (scorer)
            {
                Scored.Add(new ScoredSetEntry
                {
                    SetType = "NoCanada",
                    AmountInSet = 2,
                    PointProgression = BonusList["NoCanada"],
                    FinalPoints = 0,
                    cardsinset = new List<MineralCard>()
                });
            }
        }
        if (BonusList.ContainsKey("NoUSA"))
        {
            var scorer = true;
            foreach (var card in hand)
            {
                if (card.Location2 == "USA")
                {
                    scorer = false;
                }
            }
            if (scorer)
            {
                Scored.Add(new ScoredSetEntry
                {
                    SetType = "NoUSA",
                    AmountInSet = 2,
                    PointProgression = BonusList["NoUSA"],
                    FinalPoints = 0,
                    cardsinset = new List<MineralCard>()
                });
            }
        }
        if (BonusList.ContainsKey("USA"))
        {
            var scorer = 0;
            foreach (var card in hand)
            {
                if (card.Location2 == "USA")
                {
                    scorer++;
                }
            }

            Scored.Add(new ScoredSetEntry
            {
                SetType = "USA",
                AmountInSet = scorer,
                PointProgression = thirtyPlus,
                FinalPoints = 0,
                cardsinset = new List<MineralCard>()
            });
        }
        if (BonusList.ContainsKey("Canada"))
        {
            var scorer = 0;
            foreach (var card in hand)
            {
                if (card.Location2 == "Canada")
                {
                    scorer++;
                }
            }

            Scored.Add(new ScoredSetEntry
            {
                SetType = "Canada",
                AmountInSet = scorer,
                PointProgression = thirtyPlus,
                FinalPoints = 0,
                cardsinset = new List<MineralCard>()
            });
        }
        if (BonusList.ContainsKey("Mexico"))
        {
            var scorer = 0;
            foreach (var card in hand)
            {
                if (card.Location2 == "Mexico")
                {
                    scorer++;
                }
            }

            Scored.Add(new ScoredSetEntry
            {
                SetType = "Mexico",
                AmountInSet = scorer,
                PointProgression = thirtyPlus,
                FinalPoints = 0,
                cardsinset = new List<MineralCard>()
            });
        }
        if (BonusList.ContainsKey("Multicolored"))
        {
            var scorer = 0;
            foreach (var card in hand)
            {
                var realColors = card.Colors.Where(c => !string.IsNullOrEmpty(c)).ToList();

                if (realColors.Count >= 2)
                {
                    scorer++;
                }
            }

            Scored.Add(new ScoredSetEntry
            {
                SetType = "Multicolored",
                AmountInSet = scorer,
                PointProgression = thirtyPlus,
                FinalPoints = 0,
                cardsinset = new List<MineralCard>()
            });
        }


        foreach (var set in Scored.ToList())
        {
            CalculateScore(set, goalCardNumbers);
            //     Debug.Log(set.FinalPoints);
            score += set.FinalPoints;
            //     Debug.Log(score);
        }

        // Remember to add your money calculation rule here
        score += leftoverMoney;


        return score;
    }
}