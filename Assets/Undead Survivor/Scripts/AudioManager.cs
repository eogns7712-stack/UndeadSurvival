using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;    // 정적 메모리에 담기 위한 instance 변수 생성

    [Header("#BGM")]    // 배경음과 관련된 클립, 볼륨, 오디오소스 변수 생성
    public AudioClip bgmClip;
    public float bgmVolume;
    AudioSource bgmPlayer;
    AudioHighPassFilter bgmEffect;  // 오디오 하이패스를 사용하기 위한 변수 생성

    [Header("#SFX")]    // 효과음과 관련된 변수 생성
    public AudioClip[] sfxClips;
    public float sfxVolume;
    public int channels;// 다량의 효과음을 낼 수 있도록 채널 개수 변수 선언
    AudioSource[] sfxPlayers;
    int channelIndex;

    public enum Sfx { Dead, Hit, LevelUp=3, Lose, Melee, Range=7, Select, Win} // 효과음과 1:1 대응하는 열거형 데이터 선언

    void Awake()
    {
        instance = this;
        Init();
    }

    void Init()
    {
        // 배경음 플레이어 초기화
        GameObject bgmObject = new GameObject("BgmPlayer"); // 새로운 게임오브젝트 'BgmObject' 생성
        bgmObject.transform.parent = transform; // 생성된  BgmObject의 부모 오브젝트는 AudioManager
        bgmPlayer = bgmObject.AddComponent<AudioSource>();  // AddComponent함수로 오디오소스를 생성하고 변수에 저장
        bgmPlayer.playOnAwake = false;  // 게임을 켜자마자 재생되면 안되기 때문에 기본값 false로 지정
        bgmPlayer.loop = true;  // bgm 무한재생을 위한 loop값 true
        bgmPlayer.volume = bgmVolume;   // 소리 크기는 미리 지정했던 bgmVolume값
        bgmPlayer.clip = bgmClip;   // 재생할 BgmClip 지정
        bgmEffect = Camera.main.GetComponent<AudioHighPassFilter>();
        
        // 효과음 플레이어 초기화
        GameObject sfxObject = new GameObject("SfxPlayer");
        sfxObject.transform.parent = transform;
        sfxPlayers = new AudioSource[channels]; // 채널값을 사용해 오디오소스 배열 초기화, 효과음소스는 채널 갯수만큼 여러개
        
        for (int index = 0; index < sfxPlayers.Length; index++)
        {
            sfxPlayers[index] = sfxObject.AddComponent<AudioSource>();  // 반복문으로 모든 효과음 오디오소스 생성하면서 저장
            sfxPlayers[index].playOnAwake = false;
            sfxPlayers[index].volume = sfxVolume;
            sfxPlayers[index].bypassListenerEffects = true; // AudioSource가 AudioListener의 효과를 무시하도록 설정
        }
    }

    public void PlayBgm(bool isPlay)    // 배경음(bgm)을 재생하는 함수 작성
    {
        if (isPlay)
        {
            bgmPlayer.Play();
        }
        else
        {
            bgmPlayer.Stop();
        }
    }

    public void EffectBgm(bool isPlay)    // 오디오 하이패스를 사용하기 위한 함수 작성
    {
            bgmEffect.enabled = isPlay;
    }
    public void PlaySfx(Sfx sfx)    // 효과음 재생함수 작성
    {
        for (int index = 0; index < sfxPlayers.Length; index++)
        {
            int loopIndex = (index + channelIndex) % sfxPlayers.Length;    // 채널 개수만큼 순회하도록 채널인덱스 변수 활용, 배열 범위를 넘지 않도록 나머지연산 사용

            if (sfxPlayers[loopIndex].isPlaying)    // 이미 재생중인 효과음을 끊지않으면서 다음 루프로 넘어가도록 설정
                continue;   // continue : 반복문 도중 다음루프로 건너뛰는 키워드

            int ranIndex = 0;
            if (sfx == Sfx.Hit || sfx == Sfx.Melee) // 효과음이 2개 이상인 것은 랜덤인덱스 더하기, 만약 효과음종류의 갯수가 서로 다르다면 switch로 분리
            {
                ranIndex = Random.Range(0,2);
            }

            channelIndex = loopIndex;
            sfxPlayers[loopIndex].clip = sfxClips[(int)sfx];
            sfxPlayers[loopIndex].Play();  // 오디오소스의 클립을 변경하고 Play 함수 호출
            break;  // 효과음 재생이 된 후 반복문 종료, 여러개의 플레이어가 같은 클립을 재생하는것을 방지
        }
    }
}
