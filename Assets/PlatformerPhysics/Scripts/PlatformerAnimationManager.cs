using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlatformerController))]
public class PlatformerAnimationManager : MonoBehaviour {

    private Animator _Animator;

    private PlatformerController _Controller;

	// Use this for initialization
	void Start () {
		_Controller = GetComponent<PlatformerController>();
        _Animator = GetComponent<Animator>();
	}
	
	// Update is called once per frame
	void Update () {
        _Animator.SetBool("Grounded", _Controller.GetGrounded());
        _Animator.SetFloat("Speed", Mathf.Abs(_Controller.GetVelocity().x));
	}
}
