using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
	public float moveSpeed = 5f;
	public float sprintMultiplier = 2f;
	public float acceleration = 10f;
	[Tooltip("관성(감쇠) 계수. 0에 가까울수록 미끄러지듯 멈춤, 1에 가까울수록 즉시 멈춤")]
	[Range(0f, 1f)]
	public float inertia = 0.9f;
	private Vector3 currentVelocity = Vector3.zero;

	void Update()
	{
		float moveX = Input.GetAxisRaw("Horizontal");
		float moveZ = Input.GetAxisRaw("Vertical");
		Vector3 moveInput = new Vector3(moveX, 0, moveZ).normalized;

		float speed = moveSpeed;
		if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
		{
			speed *= sprintMultiplier;
		}

		Vector3 targetVelocity = moveInput * speed;
		currentVelocity = Vector3.MoveTowards(currentVelocity, targetVelocity, acceleration * Time.deltaTime);

		// 관성 적용: 입력이 없을 때 점진적으로 멈춤
		if (moveInput == Vector3.zero)
		{
			currentVelocity *= inertia;
		}

		transform.position += currentVelocity * Time.deltaTime;
	}
}
