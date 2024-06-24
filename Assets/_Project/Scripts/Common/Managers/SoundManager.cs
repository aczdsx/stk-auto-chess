using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public enum SoundBGM
{
    NONE = -1,

    snd_bgm_lobby,
    snd_bgm_chapter0,
    snd_bgm_chapter1,
    snd_bgm_chapter2,
}

public enum SoundFX
{
    //Common 0~ 1000
    NONE = -1,
    UnknownSound = 0,
    sfx_battle_start,
    sfx_cancel,
    sfx_click,
    sfx_popup,
    sfx_popup2,
    sfx_tab,
    sfx_card_flip,
    sfx_reinforce,
    sfx_equip,
    sfx_exp_up,
    sfx_item_show_special,
    sfx_item_buy,
    sfx_item_sell,
    sfx_change_weapon,
    sfx_card_draw,
    sfx_shake,
    sfx_congrats,
    SFX_Ancient_Weapon_Gacha,
    SFX_Ancient_Weapon_Gacha_Base,
    SFX_Ancient_Weapon_Gacha_Common,
    SFX_Ancient_Weapon_Gacha_Legend,
    SFX_Ancient_Weapon_Gacha_Ancient,

    // Enemy
    sfx_enemy_hit = 101,

    // Player
    sfx_attack_axe = 201,
    sfx_attack_bow,
    sfx_attack_dagger,
    sfx_attack_gun,
    sfx_attack_laser,
    sfx_attack_magic,
    sfx_attack_punch,
    sfx_attack_sword,

    // UI & Home
    sfx_enhance = 301,
    sfx_levelup,
    sfx_reward,
    sfx_building_start,
    sfx_building_complete,
    sfx_stone_upgrade,
    sfx_stone_upgrade_fail,
    sfx_text,
    sfx_hero_set,
    sfx_promotion,
    sfx_victory,
    sfx_knights_levelup,
    sfx_chestOpenFx,
    sfx_legendGachaPop,
    sfx_gacha_complete,
    sfx_rune_levelup,
    sfx_ChapterBridgePop,
    sfx_artifact_reinforce_success,
    sfx_artifact_reinforce_fail,
    sfx_challange_shop_shuffle_1t,
    sfx_challenge_shop_shuffle_10t,
    SFX_Guild_Entrance_First,

    // VOX
    TitleCall = 401,
    TitleCall_JP,

    // AMB
    amb_tavern = 501,
    amb_night,
    amb_rainy,
    amb_wind
}


public class SoundManager : GameObjectSingleton<SoundManager>
{
    /////////////////////////////////////////////////////////////
    // public

    public bool IsReady => this.isReady;

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
            return this.PlaySFXWithoutSilence(voxString);
        else
            return this.PlaySFX(voxString);
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
        int volume = Convert.ToInt32((-80f + v * 80f) * 0.5f);
        if (this.isReady)
            _mixer.SetFloat("BGM", volume);
        if (v == 0)
            _mixer.SetFloat("BGM", -80f);
    }

    public void SetSFXVolume(float v)
    {
        int volume = Convert.ToInt32((-80f + v * 80f) * 0.5f);
        if (this.isReady)
        {
            _mixer.SetFloat("SFX", volume);
            _mixer.SetFloat("AMB", volume);
        }

        if (v == 0)
        {
            _mixer.SetFloat("SFX", -80f);
            _mixer.SetFloat("AMB", -80f);
        }
    }

    public void SetVOXVolume(float v)
    {
        int volume = Convert.ToInt32((-80f + v * 80f) * 0.5f);
        if (this.isReady)
            _mixer.SetFloat("VOX", volume);
        if (v == 0)
            _mixer.SetFloat("VOX", -80f);
    }

    public void SetVOXUIVolume(float v)
    {
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

    protected override void Awake()
    {
        base.Awake();
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
        this.onBGM = Preference.LoadPreference(Pref.BGM_V, true);
        this.onSFX = Preference.LoadPreference(Pref.SFX_V, true);
        this.onSFX = Preference.LoadPreference(Pref.VOX_V, true);
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

        return AudioController.PlayMusic(audioID);
    }

    private ClockStone.AudioObject PlaySFX(string audioID)
    {
        if (!this.isReady) return null;

        if (!this.onSFX)
            return null;

        if (this.isSilence)
            return AudioController.Play(audioID, 0.2f);
        else
            return AudioController.Play(audioID);
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
}
