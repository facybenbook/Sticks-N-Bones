﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour {

    [SerializeField] float dashSpeed = 9.0f;
    [SerializeField] float skipSpeed = 1.23f;
    [SerializeField] float jumpVelocity = 12.0f;
    [SerializeField] float dashbackUpVelocity = 8.0f;
    [SerializeField] PlayerRole role = PlayerRole.Local;

    private Animator playerAnimator;
    private SNBPlayer player = SNBGlobal.thisPlayer;

    void Start() {
        playerAnimator = GetComponent<Animator>();

        if (role == PlayerRole.Local) {
            player.state.OnComboEvent += HandleComboEvent;
        }
    }

    void Update() {
        if (role == PlayerRole.Local) {
            RespondToHAxis();
            CharacterJump();
            LookAtOpponent();
        }
    }

    private void RespondToHAxis() {
        float horizontalMove = Input.GetAxisRaw("Horizontal");
        player.state.lastHorizontal = horizontalMove;
        Move();
    }

    private void CharacterJump() {
        if (Input.GetButtonDown("Jump") && player.state.grounded) {
            player.state.grounded = false;
            GetComponent<Rigidbody>().velocity = new Vector3(0, jumpVelocity);
            if (player.state.idle) {
                playerAnimator.CrossFade("StaticJump", 0.2f);
            } else {
                playerAnimator.CrossFade("MovingJump", 0.2f);
            }
        }
    }

    private void LookAtOpponent() {
        PlayerMovement[] players = GameObject.FindObjectsOfType<PlayerMovement>();
        foreach (PlayerMovement p in players) {
            if (p.gameObject != this.gameObject) {
                Vector3 dist = p.gameObject.transform.position - transform.position;
                transform.rotation = dist.x < 0 ? Quaternion.Euler(0, -90f, 0) : Quaternion.Euler(0, 90f, 0);  // warning: 90f might act as magic number
                player.state.facing = dist.x < 0 ? PlayerDirection.Left : PlayerDirection.Right;
            }
        }
    }

    private void Move() {
        if (player.state.lastHorizontal != 0) {
            if (player.state.idle) {
                if (MovingBack()) player.ExecuteMove(BasicMove.MoveBack);
                else player.ExecuteMove(BasicMove.Move);
            }

            if (player.state.dashing) {
                Dash();
            } else {
                if (MovingBack()) Skip(-1);
                else Skip();
            }
        } else {
            StopMoving();
        }
    }

    private bool MovingBack() {
        return (player.state.facing == PlayerDirection.Right && player.state.lastHorizontal < 0) ||
               (player.state.facing == PlayerDirection.Left && player.state.lastHorizontal > 0);
    }

    private void Dash() {
        float newX = transform.position.x + player.state.lastHorizontal * dashSpeed * Time.deltaTime;
        transform.position = new Vector3(newX, transform.position.y, transform.position.z);
        if (!player.state.dashing && player.state.grounded) {
            player.state.dashing = true;
            playerAnimator.CrossFade("Dash", 0.2f);
        }
    }

    private void DashBack() {
        GetComponent<Rigidbody>().velocity = new Vector3(0, dashbackUpVelocity);
        if (!player.state.dashing && player.state.grounded) {
            player.state.dashing = true;
            player.state.grounded = false;
            playerAnimator.CrossFade("DashBack", 0.2f);
        }
    }

    private void Skip(int direction = 1 /* for forward */) {
        float newX = transform.position.x + player.state.lastHorizontal * skipSpeed * Time.deltaTime;
        transform.position = new Vector3(newX, transform.position.y, transform.position.z);
        if (!player.state.skipping && player.state.grounded) {
            player.state.skipping = true;
            if (direction == 1) playerAnimator.CrossFade("Skip", 0.2f);
            else playerAnimator.CrossFade("SkipBack", 0.2f);
        }
    }

    private void StopMoving() {
        if (!player.state.idle) {
            player.state.dashing = player.state.skipping = false;
            playerAnimator.CrossFade("Idle", 0.2f);
        }
    }

    private void HandleComboEvent(ComboType combo) {
        switch (combo) {
            case ComboType.Dash:
                Dash();
                break;
            case ComboType.DashBack:
                DashBack();
                break;
            default:
                break;
        }
    }

    private void OnCollisionEnter(Collision collision) {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground")) {
            if (!player.state.grounded) {
                player.state.grounded = true;
                if (player.state.idle) {
                    playerAnimator.CrossFade("Idle", 0.2f);
                } else if (player.state.skipping || player.state.dashing) {
                    player.state.dashing = false;
                    playerAnimator.CrossFade("Skip", 0.2f);
                }
            }
        }
    }
}