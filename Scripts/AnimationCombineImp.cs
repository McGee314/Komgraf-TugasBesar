using Godot;
using System;

public partial class AnimationCombineImp : CharacterBody3D
{
	public const float Speed = 5.0f;
	public const float JumpVelocity = 4.5f;

	private AnimationPlayer animationPlayer;
	
	public override void _Ready()
	{
		animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		
		// Pastikan velocity 0
		Velocity = Vector3.Zero;
		
		// Mainkan animasi idle
		if (animationPlayer != null)
		{
			animationPlayer.Play("idle");
		}
	}
	
	public override void _PhysicsProcess(double delta)
	{
		// Pastikan tetap tidak bergerak
		Velocity = Vector3.Zero;
		
		// Jangan panggil MoveAndSlide() agar tidak bergerak
	}
}
