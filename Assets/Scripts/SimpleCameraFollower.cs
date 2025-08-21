using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 간단하게 target 트랜스폼을 따라 다니는 스크립트로, 이후 마우스 인풋으로 줌 확대 등을 할 때 distance, height값 + 나아가서 카메라의 pov값 등을 제어하는 식으로 수정 가능할 것이다.
/// </summary>
public class SimpleCameraFollower : MonoBehaviour
{
    [SerializeField] private Camera mainCam;
    [SerializeField] private Transform target;

    public float distance = 5f;           // The distance in the x-z plane to the target
    public float height = 1.5f;           // the height we want the camera to be above the target
    public float rotationDamping = 12.0f; // 카메라 각도 변경 시 감쇄값
    public float heightDamping = 2.0f;    // 카메라 높이 변경 시 감쇄값

    public void UpdateElapsedTime(float deltaTime)
    {
        // Calculate the current rotation angles
        var wantedRotationAngle = target.eulerAngles.y;
        var wantedHeight = target.position.y + height;

        var currentRotationAngle = mainCam.transform.eulerAngles.y;
        var currentHeight = mainCam.transform.position.y;

        // Damp the rotation around the y-axis
        currentRotationAngle = Mathf.LerpAngle(currentRotationAngle, wantedRotationAngle, rotationDamping * deltaTime);

        // Damp the height
        currentHeight = Mathf.Lerp(currentHeight, wantedHeight, heightDamping * deltaTime);

        // Convert the angle into a rotation
        var currentRotation = Quaternion.Euler(0, currentRotationAngle, 0);

        // Set the position of the camera on the x-z plane to:
        // distance meters behind the target
        mainCam.transform.position = target.position;
        mainCam.transform.position -= currentRotation * Vector3.forward * distance;

        // Set the height of the camera
        mainCam.transform.position = new Vector3(mainCam.transform.position.x, currentHeight, mainCam.transform.position.z);

        // Always look at the target
        mainCam.transform.LookAt(target);
    }
}
