using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 이 스크립트는 Rigidbody 컴포넌트가 반드시 필요함을 명시합니다.
// 만약 Rigidbody가 없으면 자동으로 추가해줍니다.
[RequireComponent(typeof(Rigidbody))]
public class ShipController : MonoBehaviour
{
    // 인스펙터 창에서 배에 가할 전진/후진 힘의 크기를 조절합니다.
    [Header("Movement Settings")]
    public float moveForce = 500f; // '속도'가 아닌 '힘'이므로 기존보다 훨씬 큰 값이 필요할 수 있습니다.

    // 인스펙터 창에서 배에 가할 회전 힘(토크)의 크기를 조절합니다.
    public float turnTorque = 250f;

    // 물리 효과 제어를 위한 Rigidbody 컴포넌트 변수
    [SerializeField] private Rigidbody rb;

    [SerializeField] private BoxCollider boxCol;

    // 사용자 입력을 저장할 변수
    private float verticalInput;
    private float horizontalInput;

    //본 스크립트가 달려있는 오브젝트가 활성화 될때 최초 1회 호출, start보다 빠르다.
    private void Awake()
    {
        Debug.LogError("Awake");
        // 이 스크립트가 붙어있는 게임 오브젝트에서 Rigidbody 컴포넌트를 찾아 rb 변수에 할당합니다.
        if (rb == null) { rb = GetComponent<Rigidbody>(); }
        
        boxCol = GetComponent<BoxCollider>();

        if (boxCol == null)
        {
            boxCol = gameObject.AddComponent<BoxCollider>();
        }

        boxCol.size = new Vector3(0.01f, 0.01f, 0.01f);
    }

    // 게임이 시작될 때 한 번 호출되는 함수입니다.
    void Start()
    {        
    }

    // 매 프레임마다 호출되는 함수입니다. 주로 입력을 받는 데 사용합니다.
    void Update()
    {
        // 1. 전진과 후진 입력 받기 (W, S 키 또는 위/아래 방향키)
        verticalInput = Input.GetAxis("Vertical");

        // 2. 좌회전과 우회전 입력 받기 (A, D 키 또는 왼쪽/오른쪽 방향키)
        horizontalInput = Input.GetAxis("Horizontal");
    }

    // 고정된 시간 간격으로 호출되는 함수입니다. 물리 계산은 여기서 해야 안정적입니다.
    void FixedUpdate()
    {
        // 3. 입력 값에 따라 배에 전진/후진 힘을 가하기
        // AddRelativeForce는 배가 바라보는 방향(로컬 좌표계)을 기준으로 힘을 가합니다.
        // Vector3.forward는 배의 '앞쪽' 방향입니다.
        // ForceMode.Force는 질량(Mass)을 고려하여 힘을 가합니다.
        if (Mathf.Abs(verticalInput) > 0.1f) // 약간의 입력에도 반응하도록
        {
            rb.AddRelativeForce(Vector3.forward * verticalInput * moveForce * Time.fixedDeltaTime);
        }

        // 4. 입력 값에 따라 배를 회전시키는 힘(토크)을 가하기
        // AddTorque는 특정 축을 기준으로 회전력을 가합니다.
        // Vector3.up은 Y축(수직축)을 기준으로 회전하겠다는 의미입니다.
        if (Mathf.Abs(horizontalInput) > 0.1f)
        {
            rb.AddTorque(Vector3.up * horizontalInput * turnTorque * Time.fixedDeltaTime);
        }
    }
}