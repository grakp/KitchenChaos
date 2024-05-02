using System;
using System.Collections;
using System.Collections.Generic;
using System.Security;
using UnityEngine;

public class Player : MonoBehaviour, IKitchenObjectParent {
    // private static Player instance;
    // public static Player Instance {
    //     get {
    //         return instance;
    //     }
    //     private set {
    //         instance = value;
    //     }
    // }
    // SAME AS
    public static Player Instance { get; private set; } // C# Property 
    public event EventHandler<OnSelectedCounterChangedEventArgs> OnSelectedCounterChanged;

    public class OnSelectedCounterChangedEventArgs : EventArgs {
        public BaseCounter selectedCounter;
    }

    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private GameInput gameInput;
    [SerializeField] private LayerMask countersLayerMask;
    [SerializeField] private Transform kitchenObjectHoldPoint;


    private bool isWalking;
    private Vector3 lastInteractDir;
    private BaseCounter selectedCounter;
    private KitchenObject kitchenObject;


    private void Awake() {
        // Safety check since we should not have >1 Player
        if (Instance != null) {
            Debug.LogError("There is more than one Player instance");
        }

        Instance = this;
    }

    private void Start() {
        gameInput.OnInteractAction += GameInput_OnInteractAction;
        gameInput.OnInteractAlternateAction += GameInput_OnInteractAlternateAction;
    }


    private void GameInput_OnInteractAction(object sender, EventArgs e) {
        if (selectedCounter != null) {
            selectedCounter.Interact(this);
        }
    }

    private void GameInput_OnInteractAlternateAction(object sender, EventArgs e) {
        if (selectedCounter != null) {
            selectedCounter.InteractAlternate(this);
        }
    }

    private void Update() {
        HandleMovement();
        HandleInteractions();
    }

    public bool IsWalking() {
        return isWalking;
    }

    private void HandleInteractions() {
        var inputVector = gameInput.GetMovementVectorNormalized();
        var moveDir = new Vector3(inputVector.x, 0f, inputVector.y);

        if (moveDir != Vector3.zero) {
            lastInteractDir = moveDir;
        }

        var interactDistance = 2f;
        // raycastHit is returned with data on hit
        if (Physics.Raycast(transform.position, lastInteractDir, out var raycastHit,
                interactDistance, countersLayerMask)) {
            // TryGetComponent() handles null
            if (raycastHit.transform.TryGetComponent(out BaseCounter baseCounter)) {
                // Has ClearCounter
                if (baseCounter != selectedCounter) {
                    SetSelectedCounter(baseCounter);
                }
            }
            else {
                selectedCounter = null;
                SetSelectedCounter(null);
            }
        }
        else {
            selectedCounter = null;
            SetSelectedCounter(null);
        }
    }

    private void HandleMovement() {
        var inputVector = gameInput.GetMovementVectorNormalized();
        var moveDir = new Vector3(inputVector.x, 0f, inputVector.y);

        var moveDistance = moveSpeed * Time.deltaTime;
        var playerRadius = .7f;
        var playerHeight = 2f;
        var canMove = !Physics.CapsuleCast(transform.position,
            transform.position + Vector3.up * playerHeight,
            playerRadius, moveDir, moveDistance);

        if (!canMove) {
            // Cannot move towards moveDir

            // Attempt only X mvt
            var moveDirX = new Vector3(moveDir.x, 0, 0).normalized; // May make more sense not to tho
            canMove = moveDir.x != 0 && !Physics.CapsuleCast(transform.position,
                transform.position + Vector3.up * playerHeight,
                playerRadius, moveDirX, moveDistance);
            if (canMove) {
                // Can only move on the X
                moveDir = moveDirX;
            }
            else {
                // Cannot move only on the X

                // Attempt only Z movement
                var moveDirZ = new Vector3(0, 0, moveDir.z).normalized;
                canMove = moveDir.z != 0 && !Physics.CapsuleCast(transform.position,
                    transform.position + Vector3.up * playerHeight,
                    playerRadius, moveDirZ, moveDistance);
                if (canMove) {
                    // Can only move on the Z
                    moveDir = moveDirZ;
                }
                else {
                    // Cannot move in any dir
                }
            }
        }

        if (canMove) {
            transform.position += moveDir * moveSpeed * Time.deltaTime;
        }


        isWalking = moveDir != Vector3.zero;
        var rotateSpeed = 10f;
        transform.forward = Vector3.Slerp(transform.forward, moveDir, Time.deltaTime * rotateSpeed);
    }

    public void SetSelectedCounter(BaseCounter selectedCounter) {
        this.selectedCounter = selectedCounter;

        OnSelectedCounterChanged?.Invoke(this, new OnSelectedCounterChangedEventArgs {
            selectedCounter = selectedCounter
        });
    }

    public Transform GetKitchenObjectFollowTransform() {
        return kitchenObjectHoldPoint;
    }

    public void SetKitchenObject(KitchenObject kitchenObject) {
        this.kitchenObject = kitchenObject;
    }

    public KitchenObject GetKitchenObject() {
        return kitchenObject;
    }

    public void ClearKitchenObject() {
        kitchenObject = null;
    }

    public bool HasKitchenObject() {
        return kitchenObject != null;
    }
}