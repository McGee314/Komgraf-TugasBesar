using Godot;
using System;

public partial class MenuAwal : Control
{
	private Button startButton;
	private Button aboutButton;
	private Button exitButton;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// Get references to the buttons
		startButton = GetNode<Button>("StartButton");
		aboutButton = GetNode<Button>("AboutButton");
		exitButton = GetNode<Button>("ExitButton");

		// Connect button signals to their respective methods
		startButton.Pressed += OnStartButtonPressed;
		aboutButton.Pressed += OnAboutButtonPressed;
		exitButton.Pressed += OnExitButtonPressed;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void OnStartButtonPressed()
	{
		// Navigate to World.tscn
		GetTree().ChangeSceneToFile("res://Scenes/World.tscn");
	}

	private void OnAboutButtonPressed()
	{
		// Placeholder for About functionality
		GD.Print("About button pressed - functionality not implemented yet");
		// You can add about dialog or scene change here later
	}

	private void OnExitButtonPressed()
	{
		// Exit the game
		GetTree().Quit();
	}
}
