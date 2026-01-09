using System;
using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle;
using UnityEngine;
using UnityEngine.Audio;

public enum SoundBGM
{
    NONE = -1,

    snd_bgm_splash_001,
    snd_bgm_lobby,
    snd_bgm_chapter0,
    snd_bgm_chapter1,
    snd_bgm_chapter2,
    snd_bgm_chapter3,
    snd_bgm_chapter4,
    snd_bgm_chapter5,
}

public enum SoundFX
{
    //Common 0~ 1000
    NONE = -1,
    UnknownSound = 0,
    sfx_battle_start,
    sfx_cancel,
    sfx_click,

    // UI
    snd_sfx_ui_btn_confirm = 101,
    snd_sfx_ui_btn_touch,
    snd_sfx_ui_btn_dialogue,
    snd_sfx_ui_btn_popup,
    snd_sfx_ui_btn_negative,
    snd_sfx_ui_item_reward,
    snd_sfx_ui_char_level_up,
    snd_sfx_ui_account_levelup,
    snd_sfx_ingame_result_victory_001,
    snd_sfx_ui_clear_star,
    snd_sfx_ingame_result_defeat_001,
    snd_sfx_ui_btn_splash,
    snd_sfx_ui_transition,
    snd_sfx_ui_btn_battle_start,
    snd_sfx_pvp_result_victory,
    snd_sfx_pvp_result_defeat,

    // Gacha
    snd_sfx_gacha_start_001 = 201,
    snd_sfx_gacha_start_002,
    snd_sfx_gacha_start_003,
    snd_sfx_gacha_start_004,
    snd_sfx_gacha_result_ambient_001,
    snd_sfx_gacha_starfall_001,
    snd_sfx_gacha_starfall_002,
    snd_sfx_gacha_open_ssr,
    snd_sfx_gacha_open_normal,
    snd_sfx_gacha_open_ticker,
    snd_sfx_gacha_open_whoosh01,
    snd_sfx_gacha_open_whoosh02,
    snd_sfx_gacha_open_cha001,

    // InGame
    snd_sfx_ingame_spawn = 301,
    snd_sfx_hit_normal1,
    snd_sfx_ingame_atkup,
    snd_sfx_ingame_spdup,
    snd_sfx_ingame_shield,
    snd_sfx_ingame_debuff,
    snd_sfx_ingame_stun,
    snd_sfx_hit_critical1,
    snd_sfx_hit_fire,
    snd_sfx_hit_ice,


    // Character Skill
    snd_sfx_skill_401011 = 401,
    snd_sfx_skill_401031,
    snd_sfx_skill_302011,
    snd_sfx_skill_404021,
    snd_sfx_skill_306011,
    snd_sfx_skill_304021,
    snd_sfx_skill_303011,
    snd_sfx_skill_403011,
    snd_sfx_skill_305011,
    snd_sfx_skill_405021,
    snd_sfx_skill_406011,
    
    // 시련
    snd_sfx_monster_204031_appear_01,
    snd_sfx_monster_204031_appear_02,
    snd_sfx_monster_204031_appear_03,

    // Monster Skill 
    // 스킬 효과음 재생 로직에 맞춰 몬스터 스킬 효과음도 snd_sfx_skill_로 시작하도록 변경하고 폴더로 구분합니다.
    //snd_sfx_monster_101011 = 501,
    //snd_sfx_monster_101021,
    //snd_sfx_monster_104011,
    //snd_sfx_monster_202011,
    //snd_sfx_monster_104021,
    //snd_sfx_monster_103011,
    //snd_sfx_monster_202021,
    //snd_sfx_monster_201011,
    //snd_sfx_monster_203011,
    //snd_sfx_monster_202031,
}


public class SoundManager : Singleton<SoundManager>
{
    /////////////////////////////////////////////////////////////
    // public

    public bool IsReady => this.isReady;
    
    public float BGMVolume { get; set; } = 1.0f;
    public float SFXVolume { get; set; } = 1.0f;
    
    public bool IsPlayingGacha { get; set; } = false;

    [SerializeField] private AudioMixer _mixer;

    public ClockStone.AudioObject PlayBGM(SoundBGM bgm)
    {
        return this.PlayBGM(bgm.ToString());
    }

    public ClockStone.AudioObject PlaySFX(SoundFX sfx, bool forceInSilence = false)
    {
        if (forceInSilence)
            return this.PlaySFXWithoutSilence(sfx.ToString());
        else
            return this.PlaySFX(sfx.ToString());
    }

    public ClockStone.AudioObject PlaySFX(string sfxString, bool forceInSilence = false)
    {
        if (forceInSilence)
            return this.PlaySFXWithoutSilence(sfxString);
        else
            return this.PlaySFX(sfxString);
    }

    //public ClockStone.AudioObject PlayVOX(SoundVOX vox, bool forceInSilence = false)
    //{
    //    if (forceInSilence)
    //        return this.PlaySFXWithoutSilence(vox.ToString());
    //    else
    //        return this.PlaySFX(vox.ToString());
    //}

    public ClockStone.AudioObject PlayAMB(SoundFX amb, bool forceInSilence = false)
    {
        if (forceInSilence)
            return this.PlayAMB(amb.ToString());
        else
            return this.PlayAMB(amb.ToString());
    }

    public ClockStone.AudioObject PlayVOX(string voxString, bool forceInSilence = false)
    {
        if (forceInSilence)
            return this.PlayVOXInternal(voxString, true);
        else
            return this.PlayVOXInternal(voxString, false);
    }

    public bool StopSFX(SoundFX sfx)
    {
        return this.StopSFX(sfx.ToString());
    }


    public bool StopBGM()
    {
        return AudioController.StopMusic();
    }

    public bool StopBGM(float fadeOut)
    {
        return AudioController.StopMusic(fadeOut);
    }

    public bool StopAMB()
    {
        return AudioController.StopAmbienceSound();
    }

    public bool StopVOX(string audioID)
    {
        if (!this.isReady) return false;

        return AudioController.Stop(audioID);
    }

    public void Silence(bool isSilence)
    {
        this.isSilence = isSilence;
    }

    public void PauseSFX()
    {
        this.onSFX = false;
    }
    public void UnPauseSFX()
    {
        this.onSFX = Preference.LoadPreference(Pref.SFX_V, true);
    }

    public void PauseBGM()
    {
        if (Preference.LoadPreference(Pref.BGM_V, 1f) > 0f)
        {
            int volume = Convert.ToInt32(-80f + 0.01f * 80f);
            _mixer.SetFloat("BGM", volume);
            _mixer.SetFloat("AMB", volume); // AMB(환경음) 볼륨 제어를 BGM에 포함
        }
    }

    public void UnPauseBGM()
    {
        if (this.isReady)
        {
            _mixer.SetFloat("BGM", Convert.ToInt32(-80f + Preference.LoadPreference(Pref.BGM_V, 1f) * 80f));
            _mixer.SetFloat("AMB", Convert.ToInt32(-80f + Preference.LoadPreference(Pref.BGM_V, 1f) * 80f)); // AMB(환경음) 볼륨 제어를 BGM에 포함
        }
        //AudioController.SetCategoryVolume("BGM", Preference.LoadPreference(Pref.BGM_V, 0.8f));
    }

    public void PauseVOX()
    {
        if (Preference.LoadPreference(Pref.VOX_V, 1f) > 0f)
        {
            int volume = Convert.ToInt32(-80f + 0.01f * 80f);
            _mixer.SetFloat("VOX", volume);
        }
    }

    public void UnPauseVOX()
    {
        if (this.isReady)
            _mixer.SetFloat("VOX", Convert.ToInt32(-80f + Preference.LoadPreference(Pref.VOX_V, 1f) * 80f));
        //AudioController.SetCategoryVolume("BGM", Preference.LoadPreference(Pref.BGM_V, 0.8f));
    }

    public void PauseVOXUI()
    {
        if (Preference.LoadPreference(Pref.VOX_V, 1f) > 0f)
        {
            int volume = Convert.ToInt32(-80f + 0.01f * 80f);
            _mixer.SetFloat("VOX_UI", volume);
        }
    }

    public void UnPauseVOXUI()
    {
        if (this.isReady)
            _mixer.SetFloat("VOX_UI", Convert.ToInt32(-80f + Preference.LoadPreference(Pref.VOX_V, 1f) * 80f));
        //AudioController.SetCategoryVolume("BGM", Preference.LoadPreference(Pref.BGM_V, 0.8f));
    }

    public void SetBGMVolume(float v)
    {
        BGMVolume = v;
        
        Preference.SavePreference(Pref.BGM_V, BGMVolume);
        
        AudioController.SetCategoryVolume("BGM", BGMVolume);
        
        // int volume = Convert.ToInt32((-80f + v * 80f) * 0.5f);
        // if (this.isReady)
        //     _mixer.SetFloat("BGM", volume);
        // if (v == 0)
        //     _mixer.SetFloat("BGM", -80f);
    }

    public void SetSFXVolume(float v)
    {
        SFXVolume = v;
        
        Preference.SavePreference(Pref.SFX_V, SFXVolume);
        
        AudioController.SetCategoryVolume("SFX", SFXVolume);
        
        // int volume = Convert.ToInt32((-80f + v * 80f) * 0.5f);
        // if (this.isReady)
        // {
        //     _mixer.SetFloat("SFX", volume);
        //     _mixer.SetFloat("AMB", volume);
        // }
        //
        // if (v == 0)
        // {
        //     _mixer.SetFloat("SFX", -80f);
        //     _mixer.SetFloat("AMB", -80f);
        // }
    }

    public void SetVOXVolume(float v)
    {
        if (!_mixer) return;

        _voxVolume = v;
        Preference.SavePreference(Pref.VOX_V, v);

        int volume = Convert.ToInt32((-80f + v * 80f) * 0.5f);
        if (this.isReady)
            _mixer.SetFloat("VOX", volume);
        if (v == 0)
            _mixer.SetFloat("VOX", -80f);
    }

    public void SetVOXUIVolume(float v)
    {
        if (!_mixer) return;
        
        int volume = Convert.ToInt32((-80f + v * 80f) * 0.5f);
        if (this.isReady)
            _mixer.SetFloat("VOX_UI", volume);
        if (v == 0)
            _mixer.SetFloat("VOX_UI", -80f);
    }

    public void SetAMBVolume(float v)
    {
        int volume = Convert.ToInt32((-80f + v * 80f) * 0.5f);
        if (this.isReady)
            _mixer.SetFloat("AMB", volume);
        if (v == 0)
            _mixer.SetFloat("AMB", -80f);
    }

    public void Initialize()
    {
        this.isReady = true;
        this.UpdateOption();
    }

    /////////////////////////////////////////////////////////////
    // Common Use

    public void PlayButtonClick()
    {
        this.PlaySFX(SoundFX.sfx_click);
    }

    public void PlayCancel()
    {
        this.PlaySFX(SoundFX.sfx_cancel);
    }

    public void UpdateOption()
    {
        // this.onBGM = Preference.LoadPreference(Pref.BGM_V, true);
        // this.onSFX = Preference.LoadPreference(Pref.SFX_V, true);
        // this.onVOX = Preference.LoadPreference(Pref.VOX_V, true);

        BGMVolume = Preference.LoadPreference(Pref.BGM_V, 1.0f);
        SFXVolume = Preference.LoadPreference(Pref.SFX_V, 1.0f);
        _voxVolume = Preference.LoadPreference(Pref.VOX_V, 1.0f);
    }


    public void StopAllSound()
    {
        AudioController.StopAll();
    }

    protected void Start()
    {
        // Run.Wait(() => { return AssetBundleManager.Instance.ReadyToStart; }, () =>
        // {
        //     AssetBundleManager.Instance.LoadAsset(AssetBundleType.SOUND, (result) =>
        //     {
        //         if (result != null)
        //         {
        //             GameObject soundManagerPrefab = AssetBundleManager.Instance.LoadObjectFromBundle(AssetBundleType.SOUND, "AudioController");
        //             GameObject bossMonsterObj = Instantiate(soundManagerPrefab, Vector3.zero, Quaternion.identity);
        //             bossMonsterObj.transform.SetParent(this.transform);
        //             this.isReady = true;
        //         }
        //     });
        // });\
        this.isReady = true;
    }

    /////////////////////////////////////////////////////////////
    // private

    private bool isSilence = false;

    private bool onBGM = true;
    private bool onSFX = true;
    private bool onAMB = true;

    private bool isReady = false;

    public ClockStone.AudioObject PlayBGM(string audioID)
    {
        if (!this.isReady) return null;

        if (!this.onBGM)
            return null;

        ClockStone.AudioObject currentAudioObj = AudioController.GetCurrentMusic();
        if (currentAudioObj != null && currentAudioObj.audioID.Equals(audioID))
            return null;

        AudioController.StopMusic();

        return AudioController.PlayMusic(audioID, BGMVolume);
    }

    private ClockStone.AudioObject PlaySFX(string audioID)
    {
        if (!this.isReady) return null;

        if (!this.onSFX)
            return null;

        if (this.isSilence)
            return AudioController.Play(audioID, 0.2f);
        else
            return AudioController.Play(audioID, SFXVolume);
    }

    private ClockStone.AudioObject PlayAMB(string audioID)
    {
        if (!this.isReady) return null;

        if (!this.onAMB)
            return null;

        ClockStone.AudioObject currentAudioObj = AudioController.GetCurrentAmbienceSound();
        if (currentAudioObj != null && currentAudioObj.audioID.Equals(audioID))
            return null;

        AudioController.StopAmbienceSound();

        return AudioController.PlayAmbienceSound(audioID);
    }

    private ClockStone.AudioObject PlaySFXWithoutSilence(string audioID)
    {
        if (!this.isReady) return null;

        if (!this.onSFX)
            return null;

        Debug.Log("PlaySFXWithoutSilence " + audioID);
        return AudioController.Play(audioID);
    }

    private bool StopSFX(string audioID)
    {
        if (!this.isReady) return false;

        return AudioController.Stop(audioID);
    }

    #region VOX (Voice)

    private bool onVOX = true;
    private float _voxVolume = 1.0f;
    public float VOXVolume
    {
        get => _voxVolume;
        set => _voxVolume = value;
    }

    private ClockStone.AudioObject PlayVOXInternal(string audioID, bool forceInSilence)
    {
        if (!this.isReady) return null;

        if (!this.onVOX)
            return null;

        float volume = forceInSilence ? 1f : (this.isSilence ? 0.2f : _voxVolume);

        // VOX 카테고리로 재생 (AudioController에 VOX 카테고리가 등록되어 있어야 함)
        // 카테고리가 없으면 기본 Play로 재생
        var audioObject = AudioController.Play(audioID, volume);

        if (audioObject != null)
        {
            Debug.Log($"PlayVOX: {audioID}, Volume: {volume}");
        }

        return audioObject;
    }

    public void PauseAllVOX()
    {
        this.onVOX = false;
    }

    public void UnPauseAllVOX()
    {
        this.onVOX = Preference.LoadPreference(Pref.VOX_V, true);
    }

    #endregion
}
