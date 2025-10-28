using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio; // AudioMixer 사용을 위해 필수
using System; // Math.Log10 사용을 위해 추가 (선택적)

public class OptionsManager : MonoBehaviour
{
    [Header("Audio Mixer")]
    public AudioMixer masterMixer; // 생성한 MainMixer 연결

    [Header("UI Sliders")]
    public Slider sfxSlider;      // 사운드 슬라이더 (SFX)
    public Slider bgmSlider;      // 배경 사운드 슬라이더 (BGM)

    // Exposed Parameter 이름은 Unity Mixer에서 설정한 이름과 일치해야 합니다.
    private const string SFX_PARAM = "SFXVolume";
    private const string BGM_PARAM = "BGMVolume";

    void Start()
    {
        // 1. 슬라이더 리스너 연결 및 초기 볼륨 로드/설정
        InitializeSlider(sfxSlider, SFX_PARAM, SetSFXVolume);
        InitializeSlider(bgmSlider, BGM_PARAM, SetBGMVolume);
    }

    // 슬라이더 초기화 및 리스너 연결을 위한 헬퍼 함수
    private void InitializeSlider(Slider slider, string paramName, Action<float> setVolumeAction)
    {
        if (slider == null) return;

        // 리스너 연결: 슬라이더 값이 바뀔 때마다 해당 볼륨 설정 함수 호출
        slider.onValueChanged.AddListener(setVolumeAction.Invoke);

        // PlayerPrefs에서 선형 값(0~1) 로드, 없으면 1.0f (100%) 사용
        float savedLinearValue = PlayerPrefs.GetFloat(paramName, 1f);

        // 슬라이더의 현재 값(Value)을 로드된 선형 값으로 설정
        slider.value = savedLinearValue;

        // Audio Mixer에 초기 볼륨 값 적용
        setVolumeAction.Invoke(savedLinearValue);
    }


    // 슬라이더 선형 값 (0~1)을 dB 값 (-80~0)으로 변환하는 함수
    private float LinearToDecibel(float linear)
    {
        // linear가 0일 경우 Mathf.Log10에서 오류가 나므로 -80f (무음)을 반환
        if (linear <= 0.0001f) return -80f;

        // 공식: 20 * log10(선형 값)
        return Mathf.Log10(linear) * 20f;
    }


    // 사운드 볼륨 설정 함수 (슬라이더 Value가 0~1 선형 값)
    public void SetSFXVolume(float linearVolume)
    {
        float dbVolume = LinearToDecibel(linearVolume);

        if (masterMixer != null)
        {
            masterMixer.SetFloat(SFX_PARAM, dbVolume);
        }

        // PlayerPrefs에 선형 값을 저장 (다음 게임 시작 시 초기값 로드용)
        PlayerPrefs.SetFloat(SFX_PARAM, linearVolume);
    }

    // 배경 사운드 볼륨 설정 함수
    public void SetBGMVolume(float linearVolume)
    {
        float dbVolume = LinearToDecibel(linearVolume);

        if (masterMixer != null)
        {
            masterMixer.SetFloat(BGM_PARAM, dbVolume);
        }

        PlayerPrefs.SetFloat(BGM_PARAM, linearVolume);
    }
}