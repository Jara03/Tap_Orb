using UnityEngine;
using UnityEngine.UI;

public class GlobalVolumeController : MonoBehaviour
{
    private const string VolumePrefKey = "GlobalVolume";

    [SerializeField] private Slider volumeSlider;
    [SerializeField] [Range(0f, 1f)] private float defaultVolume = 1f;

    private void Awake()
    {
        if (volumeSlider == null)
        {
            volumeSlider = GetComponentInChildren<Slider>(true);
        }

        float savedVolume = PlayerPrefs.GetFloat(VolumePrefKey, defaultVolume);
        ApplyVolume(savedVolume);

        if (volumeSlider != null)
        {
            volumeSlider.SetValueWithoutNotify(savedVolume);
        }
    }

    private void OnEnable()
    {
        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.AddListener(HandleSliderValueChanged);
        }
    }

    private void OnDisable()
    {
        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.RemoveListener(HandleSliderValueChanged);
        }
    }

    private void HandleSliderValueChanged(float value)
    {
        ApplyVolume(value);
    }

    private void ApplyVolume(float value)
    {
        float clampedValue = Mathf.Clamp01(value);
        AudioListener.volume = clampedValue;
        PlayerPrefs.SetFloat(VolumePrefKey, clampedValue);
        PlayerPrefs.Save();
    }
}