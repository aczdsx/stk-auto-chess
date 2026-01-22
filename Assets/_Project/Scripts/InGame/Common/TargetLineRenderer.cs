using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

public class TargetLineRenderer : MonoBehaviour
{
    public CharacterController StartCharacter => _startCharacter;

    [SerializeField] private LineRenderer _lineRenderer;
    [SerializeField] float _height = 3;
    [SerializeField] int _positionCount = 30;
    [SerializeField] float _lineDruationTime = 2;
    [SerializeField] int _offSet = 4;
    [SerializeField] ParticleSystem _ownFx;
    [SerializeField] ParticleSystem _otherFx;
    [SerializeField] GameObject _arrowFx;
    [SerializeField] TrailRenderer _trailRenderer;
    [SerializeField] Color _ownTrailColor;
    [SerializeField] Color _otherTrailColor;

    private CharacterController _startCharacter;
    private CharacterController _targetCharacter;

    public void DrawLine(CharacterController startCharacter, CharacterController targetCharacter, bool isOwn,
        Action OnComplete = null)
    {
        if (isOwn)
        {
            _lineRenderer.startColor = _ownTrailColor;
            _lineRenderer.endColor = _ownTrailColor;
            _trailRenderer.startColor = _ownTrailColor;

            _ownFx.Play();
        }
        else
        {
            _lineRenderer.startColor = _otherTrailColor;
            _lineRenderer.endColor = _otherTrailColor;
            _trailRenderer.startColor = _otherTrailColor;

            _otherFx.Play();
            // _arrowFx.transform.localScale = new Vector3(-1.5f, 0.7f, 1.5f);
        }

        _startCharacter = startCharacter;
        _targetCharacter = targetCharacter;
        _lineRenderer.gameObject.SetActive(_targetCharacter != null);

        if (gameObject.activeInHierarchy)
        {
            Vector3 startPos = _startCharacter.Position3D;
            Vector3 targetPos = _targetCharacter.Position3D;
            startPos.y += 0.5f;
            targetPos.y += 0.5f;

            StartCoroutine(DrawGuideLine(startPos, targetPos, OnComplete));
        }
    }

    public void DrawLine(Vector3 startPos, Vector3 targetPos, Action OnComplete = null)
    {
        StartCoroutine(DrawGuideLine(startPos, targetPos, OnComplete));
    }

    private IEnumerator DrawGuideLine(Vector3 startPos, Vector3 targetPos, Action OnComplete = null)
    {
        WaitForEndOfFrame waitTime = new WaitForEndOfFrame();

        List<Vector3> result = new List<Vector3>();
        for (int i = 0; i < _positionCount; i++)
        {
            float amount = Mathf.Sin(Mathf.PI * i / (_positionCount - 1)) * _height;
            Vector3 distance = targetPos - startPos;
            distance = distance * i / (_positionCount - 1) + new Vector3(0, amount, 0);
            Vector3 point = startPos + distance;
            result.Add(point);
        }

        float time = 0f;

        _lineRenderer.positionCount = _positionCount;
        for (int i = 0; i < _lineRenderer.positionCount; i++)
        {
            if (i >= result.Count)
                break;

            _lineRenderer.SetPosition(i, result[i]);
        }

        while (time < _lineDruationTime)
        {
            time += Time.unscaledDeltaTime;
            float value = time / _lineDruationTime;

            var lineIndex = (int)Mathf.Clamp((_lineRenderer.positionCount * value) + _offSet, 0, _lineRenderer.positionCount - 1);
            _arrowFx.transform.position = result[lineIndex];
            _lineRenderer.material.SetFloat("_ClipUvLeft", value);
            // _lineRenderer.material.SetFloat("_ClipUvUp", 1 - value);

            yield return waitTime;
        }

        // yield return new WaitForSeconds(1.5f);

        OnComplete?.Invoke();
    }


    public void OnEnable()
    {
        if (_trailRenderer != null)
        {
            // 활성화될 때 이전 데이터가 남지 않도록 초기화
            _trailRenderer.Clear();
            _trailRenderer.emitting = true;
        }
    }

    public void OnDisable()
    {
        if (_trailRenderer != null)
        {
            _trailRenderer.Clear();
            _trailRenderer.emitting = false;
        }
    }


    // protected void Update()
    // {
    //     if (_startCharacter && _targetCharacter)
    //     {
    //         Vector3 startPos = _startCharacter.transform.position;
    //         Vector3 targetPos = _targetCharacter.transform.position;
    //         _lineRenderer.positionCount = _positionCount;
    //         for (int i = 0; i < _positionCount; i++)
    //         {
    //             float amount = Mathf.Sin(Mathf.PI * i / (_positionCount - 1)) * _height;
    //             Vector3 distance = targetPos - startPos;
    //             distance = distance * i / (_positionCount - 1) + new Vector3(0, amount, 0);
    //             Vector3 point = startPos + distance;
    //             _lineRenderer.SetPosition(i, point);
    //         }           
    //     }
    // }
}
