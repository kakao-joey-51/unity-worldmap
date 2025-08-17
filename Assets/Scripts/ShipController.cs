using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipController : MonoBehaviour
{
    // 인스펙터 창에서 배의 전진/후진 속도를 조절할 수 있습니다.
    public float moveSpeed = 5.0f;

    // 인스펙터 창에서 배의 회전 속도를 조절할 수 있습니다.
    public float turnSpeed = 50.0f;

    // 매 프레임마다 호출되는 함수입니다.
    void Update()
    {
        // 1. 전진과 후진 입력 받기 (W, S 키 또는 위/아래 방향키)
        // GetAxis("Vertical")은 W나 위 방향키를 누르면 1, S나 아래 방향키를 누르면 -1에 가까운 값을 반환합니다.
        float verticalInput = Input.GetAxis("Vertical");

        // 2. 좌회전과 우회전 입력 받기 (A, D 키 또는 왼쪽/오른쪽 방향키)
        // GetAxis("Horizontal")은 D나 오른쪽 방향키를 누르면 1, A나 왼쪽 방향키를 누르면 -1에 가까운 값을 반환합니다.
        float horizontalInput = Input.GetAxis("Horizontal");

        // 3. 입력 값에 따라 배를 이동시키기
        // transform.forward는 배의 앞쪽 방향을 나타내는 벡터입니다.
        // 여기에 속도와 입력값, 그리고 프레임 간 시간차(Time.deltaTime)를 곱해 이동 거리를 계산합니다.
        // Time.deltaTime을 곱해주는 이유는 컴퓨터 성능과 상관없이 일정한 속도로 움직이게 하기 위함입니다.
        transform.Translate(Vector3.forward * verticalInput * moveSpeed * Time.deltaTime);

        // 4. 입력 값에 따라 배를 회전시키기
        // Vector3.up은 Y축(위쪽 방향)을 기준으로 회전하겠다는 의미입니다.
        transform.Rotate(Vector3.up * horizontalInput * turnSpeed * Time.deltaTime);
    }
}