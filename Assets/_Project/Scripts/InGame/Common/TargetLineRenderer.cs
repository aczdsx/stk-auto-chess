using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
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
            StartCoroutine(DrawGuideLine(_startCharacter, _targetCharacter, isOwn, OnComplete));
        }
    }

    private IEnumerator DrawGuideLine(CharacterController startCharacter, CharacterController targetCharacter, bool isOwn,
        Action OnComplete = null)
    {
        WaitForEndOfFrame waitTime = new WaitForEndOfFrame();
    
        Vector3 startPos = startCharacter.Position3D;
        Vector3 targetPos = targetCharacter.Position3D;
        startPos.y += 0.5f;
        targetPos.y += 0.5f;
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
            if (startCharacter == null || targetCharacter == null)
            {
                Destroy(gameObject);
            }
    
            time += Time.unscaledDeltaTime;
            float value = time / _lineDruationTime;

            var lineIndex = (int)Mathf.Clamp((_lineRenderer.positionCount * value) + _offSet, 0, _lineRenderer.positionCount - 1);
            _arrowFx.transform.position = result[lineIndex];
            _lineRenderer.material.SetFloat("_ClipUvLeft", value);
            // _lineRenderer.material.SetFloat("_ClipUvUp", 1 - value);

            yield return waitTime;
        }

        if (isOwn)
        {
            if (targetCharacter != null)
            {
                // _arrowFx.gameObject.SetActive(false);
                // _ownFx.gameObject.SetActive(true);
                // _ownFx.transform.position = targetPos;
                // _ownFx.Play();
            }
        }
        else
        {
            if (targetCharacter != null)
            {
                // _arrowFx.gameObject.SetActive(false);
                // _otherFx.gameObject.SetActive(true);
                // _otherFx.transform.position = targetPos;
                // _otherFx.Play();
            }
        }
        
        yield return new WaitForSeconds(1.5f);
        
        OnComplete?.Invoke();
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
