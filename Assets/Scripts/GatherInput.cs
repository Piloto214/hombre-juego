using UnityEngine;
using UnityEngine.InputSystem;

public class GatherInput : MonoBehaviour
{
    private Controls controls;

    [SerializeField] private float _valueX;
    public float ValueX { get => _valueX; }

    [SerializeField] private bool _isJumping;
    public bool IsJumping { get => _isJumping; set => _isJumping = value; }

    [SerializeField] private bool _isAttacking;
    public bool IsAttacking { get => _isAttacking; set => _isAttacking = value; }

    [SerializeField] private bool _isDashing;
    public bool IsDashing { get => _isDashing; set => _isDashing = value; }

    [SerializeField] private bool _isShooting;
    public bool IsShooting { get => _isShooting; set => _isShooting = value; }

    private void Awake()
    {
        controls = new Controls();
    }

    private void OnEnable()
    {
        controls.Player.Move.performed += StartMove;
        controls.Player.Move.canceled += StopMove;
        controls.Player.Jump.performed += StartJump;
        controls.Player.Jump.canceled += StopJump;
        controls.Player.Attack.performed += startAttack;
        controls.Player.Attack.canceled += stopAttack;
        controls.Player.Dash.performed += StartDash;
        controls.Player.Dash.canceled += StopDash;
        controls.Player.Shoot.performed += StartShoot;
        controls.Player.Shoot.canceled += StopShoot;

        controls.Player.Enable();
    }

    private void StartMove(InputAction.CallbackContext context)
    {
        _valueX = context.ReadValue<float>();
    }

    private void StopMove(InputAction.CallbackContext context)
    {
        _valueX = 0;
    }

    private void StartJump(InputAction.CallbackContext context)
    {
        _isJumping = true;
    }

    private void StopJump(InputAction.CallbackContext context)
    {
        _isJumping = false;
    }

    private void startAttack(InputAction.CallbackContext context)
    {
        _isAttacking = true;
    }

    private void stopAttack(InputAction.CallbackContext context)
    {
        _isAttacking = false;
    }

    private void StartDash(InputAction.CallbackContext context)
    {
        _isDashing = true;
    }

    private void StopDash(InputAction.CallbackContext context)
    {
        _isDashing = false;
    }

    private void StartShoot(InputAction.CallbackContext context)
    {
        _isShooting = true;
    }

    private void StopShoot(InputAction.CallbackContext context)
    {
        _isShooting = false;
    }

    private void OnDisable()
    {
        if (controls == null) return;
        controls.Player.Move.performed -= StartMove;
        controls.Player.Move.canceled -= StopMove;
        controls.Player.Jump.performed -= StartJump;
        controls.Player.Jump.canceled -= StopJump;
        controls.Player.Attack.performed -= startAttack;
        controls.Player.Attack.canceled -= stopAttack;
        controls.Player.Dash.performed -= StartDash;
        controls.Player.Dash.canceled -= StopDash;
        controls.Player.Shoot.performed -= StartShoot;
        controls.Player.Shoot.canceled -= StopShoot;

        controls.Player.Disable();
    }
}