using System;
using UnityEngine;

public class CameraMove : MonoBehaviour
{
    public float rotateXSpeed = 1f;
    public float rotateYSpeed = 1f;
    
    private bool _isRotate;
    private Vector3 _traStart;
    private Vector3 _mouseStart;
    private bool _isDown;

    private void Start()
    {
        ResetRotation();
    }

    private void Update()
    {
        var thisTransform = transform;
        
        if (_isRotate && Input.GetMouseButtonUp(1))
            _isRotate = false;

        if (_isRotate) 
        {
            var offset = Input.mousePosition - _mouseStart;
            
            // whether the lens is facing down
            if (_isDown)
            {   var rot = _traStart + new Vector3(offset.y * 0.3f * rotateYSpeed, -offset.x * 0.3f * rotateXSpeed, 0);
                rot.x = Mathf.Clamp(rot.x, 0f, 90f);
                thisTransform.rotation = Quaternion.Euler(rot);
            }
            else
            {
                var rotNotDown = _traStart + new Vector3(-offset.y * 0.3f * rotateYSpeed, offset.x * 0.3f * rotateXSpeed, 0);
                rotNotDown.x = Mathf.Clamp(rotNotDown.x, 0f, 90f); 
                thisTransform.rotation = Quaternion.Euler(rotNotDown);
            }
        }

        else if (Input.GetMouseButtonDown(1))
        {
            _isRotate = true;
            _mouseStart = Input.mousePosition;
            _traStart = thisTransform.rotation.eulerAngles;
            _isDown = thisTransform.up.y < -0.0001f;
        }
    }

    public void ResetRotation()
    {
        transform.rotation = GameManager.Color == "White"
            ? Quaternion.Euler(new Vector3(60, 0, 0))
            : Quaternion.Euler(new Vector3(60, 180, 0));
    }
}