using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UIElements;

public class button : MonoBehaviour
{
    [SerializeField] private TMP_InputField cardInputField;
    [SerializeField] private TMP_Text placeholderText;
    [SerializeField] private TMP_Text FinalScore;
    [SerializeField] private TMP_Text Title;
    [SerializeField] private GameObject SetsList;

    // These variables act exactly like your Scratch storage variables
    private int totalCardsExpected = 0;
    private int cardsIndex = 1;
    private int totalGoalCardsExpected = 5;
    private int goalCardsIndex = 1;



    public int leftoverCash = 0;
    public bool advancedMode = false;
    public List<int> cardsOwned = new List<int>();
    public List<int> goalCards = new List<int>();
    Program Calculator = null;


    // This enum maps your prompt steps cleanly
    private enum InputState { EnteringCash, EnteringAdvancedMode, EnteringGoalCards, EnteringTotalCount, EnteringCards, Complete }
    private InputState currentState = InputState.EnteringCash;

    private void Start()
    {
        Calculator = GetComponent<Program>();
        cardInputField.gameObject.SetActive(false);
        Title.gameObject.SetActive(false);
        FinalScore.gameObject.SetActive(false);
        SetsList.gameObject.SetActive(false);
    }

    // THIS IS THE ONLY SUBMIT FUNCTION NEEDED:
    // Notice there are ZERO lines here calling the broken "Input.GetKeyDown" method!
    private void OnInputSubmit(string text)
    {
        ProcessCurrentState();
    }

    private void ProcessCurrentState()
    {
        if (cardInputField == null || placeholderText == null) return;

        string rawText = cardInputField.text;
        cardInputField.text = ""; // Instantly clear the box for the next input

        // 1. STATE: ENTERING CASH
        if (currentState == InputState.EnteringCash)
        {
            int.TryParse(rawText, out leftoverCash);

            // Move to the next Scratch block prompt!
            currentState = InputState.EnteringAdvancedMode;
            placeholderText.text = "Are You Playing In Advanced Mode?(Y/N)";
        }
        else if (currentState == InputState.EnteringAdvancedMode)
        {
            if (rawText.ToLower() == "y")
            {
                advancedMode = true;
                goalCards.Clear();

                // Advance to cabinet entry phase instantly
                currentState = InputState.EnteringTotalCount;
                placeholderText.text = "How many cards are in your cabinet?";
            }
            else
            {
                advancedMode = false;
                goalCards.Clear();
                goalCardsIndex = 1; // Explicitly ensure counter starts at 1

                // Advance to goal card entry phase instead
                currentState = InputState.EnteringGoalCards;
                placeholderText.text = $"Goal Card {goalCardsIndex}:";
            }
        }
        else if (currentState == InputState.EnteringGoalCards)
        {
            if (int.TryParse(rawText, out int cardNumber))
            {
                goalCards.Add(cardNumber);

                goalCardsIndex++;
            }

            if (goalCards.Count >= totalGoalCardsExpected)
            {
                currentState = InputState.EnteringTotalCount;
                placeholderText.text = "How many cards are in your cabinet?";
            }
            else
            {
                placeholderText.text = $"Goal Card {goalCardsIndex}:";
            }
        }
        // 2. STATE: ENTERING TOTAL CARD COUNT
        else if (currentState == InputState.EnteringTotalCount)
        {
            int.TryParse(rawText, out totalCardsExpected);

            if (totalCardsExpected <= 0)
            {
                // Edge case safety: If they have 0 cards, go straight to complete!
                TriggerCompleteScoring();
                return;
            }

            // Set up your Scratch repeat loop parameters!
            cardsIndex = 1;
            cardsOwned.Clear();
            currentState = InputState.EnteringCards;
            placeholderText.text = $"Card {cardsIndex}:";
        }
        // 3. STATE: THE REPEAT LOOP PASS
        else if (currentState == InputState.EnteringCards)
        {
            if (int.TryParse(rawText, out int cardNumber))
            {
                // Equivalent to: "add answer to Cards owned"
                cardsOwned.Add(cardNumber);

                // Equivalent to: "change Cards index by 1"
                cardsIndex++;
            }

            // Check if the repeat loop has run the expected amount of times!
            if (cardsOwned.Count >= totalCardsExpected)
            {
                TriggerCompleteScoring();
            }
            else
            {
                // Keep looping and update the prompt text: "Card 2:", "Card 3:", etc.
                placeholderText.text = $"Card {cardsIndex}:";
            }
        }

        // Keep the cursor flashing inside the box so they don't have to keep clicking it
        cardInputField.ActivateInputField();
    }

    // Equivalent to your pink custom "Complete" block!
    private void TriggerCompleteScoring()
    {
        currentState = InputState.Complete;
        placeholderText.text = "Calculating final score...";
        cardInputField.gameObject.SetActive(false); // Hide the typing box since inputs are done!

     //   Debug.Log($"Cash: {leftoverCash} | Hand Size: {cardsOwned.Count}");

        int score = Calculator.Main();

        FinalScore.text = score.ToString();

        FinalScore.gameObject.SetActive(true);
        Title.gameObject.SetActive(true);
        SetsList.gameObject.SetActive(true);
    }
    public void OnClick()
    {
        currentState = InputState.EnteringCash;
        leftoverCash = 0;
        totalCardsExpected = 0;
        cardsIndex = 1;
        totalGoalCardsExpected = 5;
        goalCardsIndex = 1;
        advancedMode = false;
        cardsOwned.Clear();
        goalCards.Clear();
        // Equivalent to your first block step!
        if (placeholderText != null)
        {
            placeholderText.text = "How much cash do you have left over?";
        }

        if (cardInputField != null)
        {
            cardInputField.text = "";
            cardInputField.onSubmit.RemoveAllListeners();

            cardInputField.onSubmit.AddListener(OnInputSubmit);
            cardInputField.ActivateInputField();
        }
        cardInputField.gameObject.SetActive(true);
    }
}