using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode] // 에디터에서도 실시간으로 확인 가능하게 합니다.
public class ParticleAlphaController : MonoBehaviour
{
    [Range(0f, 1f)]
    public float groupAlpha = 1f;

    private ParticleSystem[] cachedParticles;
    private float lastAlpha = -1f;

    void Start()
    {
        RefreshCache();
    }

    void Update()
    {
        // 값이 변경되었을 때만 실행하여 최적화합니다.
        if (Mathf.Approximately(groupAlpha, lastAlpha)) return;

        UpdateParticleAlpha();
        lastAlpha = groupAlpha;
    }

    public void RefreshCache()
    {
        // 하위의 모든 파티클 시스템을 찾아서 저장합니다.
        cachedParticles = GetComponentsInChildren<ParticleSystem>(true);
    }

    private void UpdateParticleAlpha()
    {
        if (cachedParticles == null) return;

        foreach (var ps in cachedParticles)
        {
            if (ps == null) continue;

            // 1. 앞으로 생성될 파티클의 Alpha 설정 (Main Module)
            var main = ps.main;
            Color startColor = main.startColor.color;
            startColor.a = groupAlpha;
            main.startColor = startColor;

            // 2. 이미 화면에 나와있는 파티클들의 Alpha 수정
            // 이 부분은 CPU 소모가 있을 수 있으므로 필요한 경우에만 사용하세요.
            ParticleSystem.Particle[] particles = new ParticleSystem.Particle[ps.particleCount];
            int count = ps.GetParticles(particles);

            for (int i = 0; i < count; i++)
            {
                Color c = particles[i].startColor;
                c.a = groupAlpha;
                particles[i].startColor = c;
            }

            ps.SetParticles(particles, count);
        }
    }
}