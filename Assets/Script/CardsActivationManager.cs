using UnityEngine;
using UnityEngine.UI;

public class CardsActivationManager : MonoBehaviour
{
    [SerializeField] private GameObject[] cardContainers;

    [Header("UI Buttons")]
    [SerializeField] private GameObject nextButton;
    [SerializeField] private GameObject backButton;

    private int currentIndex = 0;

    void Start()
    {
        ActivateContainer(currentIndex);
        UpdateButtons();
    }

    public void NextCardContainer()
    {
        if (currentIndex < cardContainers.Length - 1)
        {
            currentIndex++;
            ActivateContainer(currentIndex);
            UpdateButtons();
        }
    }

    public void PreviousCardContainer()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            ActivateContainer(currentIndex);
            UpdateButtons();
        }
    }

    void ActivateContainer(int index)
    {
        for (int i = 0; i < cardContainers.Length; i++)
        {
            cardContainers[i].SetActive(i == index);
        }
    }

    void UpdateButtons()
    {
        // Hide back button on first level
        backButton.SetActive(currentIndex > 0);

        // Hide next button on last level
        nextButton.SetActive(currentIndex < cardContainers.Length - 1);
    }
}