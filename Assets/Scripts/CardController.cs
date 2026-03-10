using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CanvasGroup))]
public class CardController : MonoBehaviour, IPointerClickHandler
{
    static readonly List<CardController> allCards = new List<CardController>();

    [Header("FRONT")]
    public TextMeshProUGUI frontKeyword;

    [Header("BACK")]
    public GameObject bigBack;
    public GameObject backSide;
    public Image backImage;
    public TextMeshProUGUI backInfo;

    [Header("Flip Timing")]
    public float flipDuration = 0.35f;
    public float delayBeforeFlipAnimation = 0.2f;

    [Header("Click-lock (global)")]
    public bool lockClicksForFlipDuration = true;
    public float globalClickLockSeconds = 0.35f;

    static float clicksDisabledUntil = 0f;

    [Header("Audio")]
    public AudioSource audioSource;

    [Header("Keyword Sound (front → back only)")]
    public AudioClip keywordSound;
    [Range(0f, 1f)] public float keywordVolume = 0.9f;

    [Header("Flip Sound (voice-over)")]
    public AudioClip flipSound;
    [Range(0f, 1f)] public float flipVolume = 1f;

    public bool playBothSimultaneously = false;
    public bool playFlipSoundAfterAnimation = false;
    public float extraDelayBeforeFlipSound = 0f;

    [Header("Definition Sound (front → back only)")]
    public AudioClip definitionSound;
    [Range(0f, 1f)] public float definitionVolume = 1f;

    public float delayBeforeDefinitionSound = 0.25f;

    Coroutine delayedFlipSoundRoutine;
    Coroutine delayedDefinitionRoutine;
    Coroutine delayedVoiceRoutine;

    bool isFlipped = false;
    bool flipping = false;

    void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f;
            }
        }
    }

    void OnEnable()
    {
        if (!allCards.Contains(this))
            allCards.Add(this);

        ResetCard();
    }

    void OnDisable()
    {
        if (allCards.Contains(this))
            allCards.Remove(this);

        CancelPendingAudio();
        flipping = false;
    }

    void ResetCard()
    {
        CancelPendingAudio();

        isFlipped = false;
        flipping = false;

        transform.localEulerAngles = Vector3.zero;

        if (frontKeyword != null)
            frontKeyword.gameObject.SetActive(true);

        if (backSide != null)
            backSide.SetActive(false);

        if (bigBack != null)
            bigBack.SetActive(false);
    }

    public void Setup(string keyword, Sprite infoSprite, string infoText)
    {
        frontKeyword.text = keyword;
        backImage.sprite = infoSprite;
        backInfo.text = infoText;

        ResetCard();
    }

    public void CancelPendingAudio()
    {
        if (delayedFlipSoundRoutine != null) StopCoroutine(delayedFlipSoundRoutine);
        if (delayedDefinitionRoutine != null) StopCoroutine(delayedDefinitionRoutine);
        if (delayedVoiceRoutine != null) StopCoroutine(delayedVoiceRoutine);

        delayedFlipSoundRoutine = null;
        delayedDefinitionRoutine = null;
        delayedVoiceRoutine = null;
    }

    public static void CancelAllPendingAudioOnAllCards()
    {
        foreach (var c in allCards)
            if (c != null) c.CancelPendingAudio();
    }

    static void SetGlobalClickLock(float seconds)
    {
        float until = Time.time + Mathf.Max(0f, seconds);

        if (until > clicksDisabledUntil)
            clicksDisabledUntil = until;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (Time.time < clicksDisabledUntil) return;

        if (!flipping && gameObject.activeInHierarchy)
            StartCoroutine(FlipCoroutine());
    }

    IEnumerator FlipCoroutine()
    {
        flipping = true;

        bool flippingFromFront = !isFlipped;

        float lockSeconds = globalClickLockSeconds;

        if (lockClicksForFlipDuration)
        {
            if (flippingFromFront)
                lockSeconds = (flipDuration * 2f) + delayBeforeFlipAnimation;
            else
                lockSeconds = flipDuration;
        }

        SetGlobalClickLock(lockSeconds);

        StopAllAudioSourcesInScene();
        CancelAllPendingAudioOnAllCards();
        CancelPendingAudio();

        if (audioSource != null)
        {
            if (flippingFromFront)
            {
                if (playBothSimultaneously)
                {
                    if (keywordSound) audioSource.PlayOneShot(keywordSound, keywordVolume);
                    if (flipSound) audioSource.PlayOneShot(flipSound, flipVolume);
                }
                else
                {
                    if (keywordSound)
                    {
                        audioSource.PlayOneShot(keywordSound, keywordVolume);

                        if (flipSound && !playFlipSoundAfterAnimation)
                            delayedFlipSoundRoutine = StartCoroutine(
                                PlayDelayedFlipSound(keywordSound.length + extraDelayBeforeFlipSound));
                    }
                    else if (flipSound && !playFlipSoundAfterAnimation)
                    {
                        audioSource.PlayOneShot(flipSound, flipVolume);
                    }
                }
            }
            else
            {
                if (flipSound)
                    audioSource.PlayOneShot(flipSound, flipVolume);
            }
        }

        if (flippingFromFront && delayBeforeFlipAnimation > 0)
            yield return new WaitForSeconds(delayBeforeFlipAnimation);

        float half = flipDuration / 2f;
        float t = 0f;

        while (t < half)
        {
            t += Time.deltaTime;
            float angle = Mathf.Lerp(0, 90, t / half);
            transform.localEulerAngles = new Vector3(0, angle, 0);
            yield return null;
        }

        isFlipped = !isFlipped;

        if (frontKeyword) frontKeyword.gameObject.SetActive(!isFlipped);

        if (backSide) backSide.SetActive(isFlipped);
        if (bigBack) bigBack.SetActive(isFlipped);

        if (flippingFromFront && definitionSound)
            delayedDefinitionRoutine = StartCoroutine(
                PlayDelayedDefinitionSound(delayBeforeDefinitionSound));

        if (flippingFromFront && playFlipSoundAfterAnimation && flipSound)
            delayedVoiceRoutine = StartCoroutine(
                PlayDelayedVoice(extraDelayBeforeFlipSound));

        t = 0;

        while (t < half)
        {
            t += Time.deltaTime;
            float angle = Mathf.Lerp(90, 0, t / half);
            transform.localEulerAngles = new Vector3(0, angle, 0);
            yield return null;
        }

        transform.localEulerAngles = Vector3.zero;
        flipping = false;
    }

    void StopAllAudioSourcesInScene()
    {
        foreach (var s in FindObjectsOfType<AudioSource>())
            s.Stop();
    }

    IEnumerator PlayDelayedVoice(float delay)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);

        delayedVoiceRoutine = null;

        if (audioSource && flipSound)
            audioSource.PlayOneShot(flipSound, flipVolume);
    }

    IEnumerator PlayDelayedFlipSound(float delay)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);

        delayedFlipSoundRoutine = null;

        if (audioSource && flipSound)
            audioSource.PlayOneShot(flipSound, flipVolume);
    }

    IEnumerator PlayDelayedDefinitionSound(float delay)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);

        delayedDefinitionRoutine = null;

        if (audioSource && definitionSound)
            audioSource.PlayOneShot(definitionSound, definitionVolume);
    }
}