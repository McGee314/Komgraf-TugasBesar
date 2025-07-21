using Godot;

public partial class Indomie : CharacterBody3D
{
	// Pickup Settings
	[Export] private bool CanBePickedUp = true;
	[Export] private string ItemName = "Indomie";
	[Export] private string ItemDescription = "Mie instan yang lezat";
	
	// Highlight Settings
	[Export] private Color HighlightColor = Colors.Yellow;
	[Export] private float HighlightIntensity = 1.5f;
	
	// Audio Settings
	[Export] private AudioStream PickupSound;
	[Export] private AudioStream DropSound;
	
	// Components
	private MeshInstance3D _meshInstance;
	private CollisionShape3D _collisionShape;
	private AudioStreamPlayer3D _audioPlayer;
	
	// Highlight state
	private bool _isHighlighted = false;
	private Material _originalMaterial;
	private StandardMaterial3D _highlightMaterial;
	
	// Pickup state
	private bool _isPickedUp = false;
	private Node3D _currentHolder = null;
	
	public override void _Ready()
	{
		GD.Print($"🍜 {ItemName} loaded!");
		
		// Add to pickupable group
		AddToGroup("Pickupable");
		
		// Setup components
		SetupComponents();
		SetupHighlightMaterial();
		SetupPhysics();
		
		GD.Print($"✅ {ItemName} ready for pickup!");
	}
	
	private void SetupComponents()
	{
		// Find MeshInstance3D in children (might be nested)
		_meshInstance = FindMeshInstance(this);
		if (_meshInstance != null)
		{
			GD.Print($"📦 Mesh found: {_meshInstance.Name}");
			// Store original material
			if (_meshInstance.MaterialOverride != null)
			{
				_originalMaterial = _meshInstance.MaterialOverride;
			}
			else if (_meshInstance.GetSurfaceOverrideMaterial(0) != null)
			{
				_originalMaterial = _meshInstance.GetSurfaceOverrideMaterial(0);
			}
		}
		else
		{
			GD.Print("⚠️ No MeshInstance3D found in children");
		}
		
		// Find CollisionShape3D
		_collisionShape = GetNode<CollisionShape3D>("CollisionShape3D");
		if (_collisionShape != null)
		{
			GD.Print("✅ CollisionShape3D found");
		}
		else
		{
			GD.Print("⚠️ CollisionShape3D not found");
		}
		
		// Setup AudioStreamPlayer3D
		_audioPlayer = GetNodeOrNull<AudioStreamPlayer3D>("AudioStreamPlayer3D");
		if (_audioPlayer == null)
		{
			_audioPlayer = new AudioStreamPlayer3D();
			_audioPlayer.Name = "AudioStreamPlayer3D";
			AddChild(_audioPlayer);
		}
	}
	
	private MeshInstance3D FindMeshInstance(Node node)
	{
		// Check if current node is MeshInstance3D
		if (node is MeshInstance3D meshInstance)
		{
			return meshInstance;
		}
		
		// Search in children recursively
		foreach (Node child in node.GetChildren())
		{
			var result = FindMeshInstance(child);
			if (result != null)
				return result;
		}
		
		return null;
	}
	
	private void SetupHighlightMaterial()
	{
		// Create highlight material
		_highlightMaterial = new StandardMaterial3D();
		_highlightMaterial.AlbedoColor = HighlightColor;
		_highlightMaterial.EmissionEnabled = true;
		_highlightMaterial.Emission = HighlightColor * HighlightIntensity;
		// _highlightMaterial.EmissionEnergy = 0.5f;
		_highlightMaterial.Roughness = 0.3f;
		_highlightMaterial.Metallic = 0.1f;
	}
	
	private void SetupPhysics()
	{
		// Set collision layers
		CollisionLayer = 2; // Layer 2 for pickup items
		CollisionMask = 1;  // Can collide with world (layer 1)

	}
	
	// Method called by pickup system to check if can be picked up
	public bool CanBePickedUpMethod()
	{
		return CanBePickedUp && !_isPickedUp;
	}
	
	// Method called by pickup system for highlighting
	public void Highlight(bool enable)
	{
		if (_meshInstance == null) return;
		
		_isHighlighted = enable;
		
		if (enable)
		{
			// Apply highlight material
			if (_meshInstance.MaterialOverride != null)
			{
				_meshInstance.MaterialOverride = _highlightMaterial;
			}
			else
			{
				_meshInstance.SetSurfaceOverrideMaterial(0, _highlightMaterial);
			}
			
			GD.Print($"✨ Highlighting {ItemName}");
		}
		else
		{
			// Restore original material
			if (_originalMaterial != null)
			{
				if (_meshInstance.MaterialOverride != null)
				{
					_meshInstance.MaterialOverride = _originalMaterial;
				}
				else
				{
					_meshInstance.SetSurfaceOverrideMaterial(0, _originalMaterial);
				}
			}
			else
			{
				// Remove material override
				_meshInstance.MaterialOverride = null;
				_meshInstance.SetSurfaceOverrideMaterial(0, null);
			}
		}
	}
	
	// Method called when object is picked up
	public void OnPickedUp(Node3D holder)
	{
		_isPickedUp = true;
		_currentHolder = holder;
		
		// Play pickup sound
		PlaySound(PickupSound);
		
		// Disable highlight when picked up
		Highlight(false);
		
		GD.Print($"🤏 {ItemName} picked up by {holder.Name}");
		
		// Optional: Add pickup effects
		CreatePickupEffect();
	}
	
	// Method called when object is dropped
	public void OnDropped(Node3D holder)
	{
		_isPickedUp = false;
		_currentHolder = null;
		
		// Play drop sound
		PlaySound(DropSound);
		
		GD.Print($"🗑️ {ItemName} dropped by {holder.Name}");
		
		// Optional: Add drop effects
		CreateDropEffect();
		
		// CharacterBody3D tidak memiliki Freeze dan ApplyImpulse
		// Ganti dengan:
		if (IsInstanceValid(this))
		{
			// Set posisi sedikit ke atas saat di-drop
			GlobalPosition += Vector3.Up * 0.1f;
			// Atau gunakan Velocity untuk memberikan efek jatuh
			Velocity = Vector3.Up * 2.0f;
		}
	}
	
	// Method to enable/disable collision (called by pickup system)
	public void SetCollisionEnabled(bool enabled)
	{
		if (_collisionShape != null)
		{
			_collisionShape.Disabled = !enabled;
		}
		
		// Also set RigidBody collision
		if (enabled)
		{
			CollisionLayer = 2;
			CollisionMask = 1;
		}
		else
		{
			CollisionLayer = 0;
			CollisionMask = 0;
		}
	}
	
	private void PlaySound(AudioStream sound)
	{
		if (_audioPlayer != null && sound != null)
		{
			_audioPlayer.Stream = sound;
			_audioPlayer.Play();
		}
	}
	
	private void CreatePickupEffect()
	{
		// Simple particle effect - you can expand this
		var tween = GetTree().CreateTween();
		var originalScale = Scale;
		
		// Scale up then down quickly
		tween.TweenProperty(this, "scale", originalScale * 1.2f, 0.1f);
		tween.TweenProperty(this, "scale", originalScale, 0.1f);
	}
	
	private void CreateDropEffect()
	{
		// Simple drop effect
		var tween = GetTree().CreateTween();
		
		// Quick rotation effect
		tween.TweenProperty(this, "rotation", Rotation + new Vector3(0, Mathf.Pi * 0.5f, 0), 0.3f);
	}
	
	// Interaction when clicked or touched
	public override void _InputEvent(Camera3D camera, InputEvent @event, Vector3 position, Vector3 normal, int shapeIdx)
	{
		if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
		{
			if (mouseEvent.ButtonIndex == MouseButton.Left)
			{
				GD.Print($"🖱️ Clicked on {ItemName}");
				// Optional: Add click feedback
				CreateClickEffect();
			}
		}
	}
	
	private void CreateClickEffect()
	{
		// Simple click feedback
		if (_meshInstance != null)
		{
			var tween = GetTree().CreateTween();
			var originalScale = _meshInstance.Scale;
			
			tween.TweenProperty(_meshInstance, "scale", originalScale * 0.9f, 0.05f);
			tween.TweenProperty(_meshInstance, "scale", originalScale, 0.05f);
		}
	}
	
	// Public getters for item info
	public string GetItemName() => ItemName;
	public string GetItemDescription() => ItemDescription;
	public bool IsPickedUp() => _isPickedUp;
	public Node3D GetCurrentHolder() => _currentHolder;
	
	// Method to reset object state (useful for respawning)
	public void ResetObject()
	{
		_isPickedUp = false;
		_currentHolder = null;
		Highlight(false);
		
		// Reset physics - CharacterBody3D style
		SetCollisionEnabled(true);
		
		// Reset velocity instead of AngularVelocity dan LinearVelocity
		Velocity = Vector3.Zero;
		
		GD.Print($"🔄 {ItemName} reset");
	}
	
	// Optional: Auto-highlight when player is near
	public override void _PhysicsProcess(double delta)
	{
		// You can add proximity-based highlighting here if needed
		// This is optional and can be removed if not needed
		
		// Example: Auto-highlight when player is very close
		if (!_isPickedUp && !_isHighlighted)
		{
			// Find player in scene
			var player = GetTree().GetFirstNodeInGroup("Player");
			if (player != null)
			{
				float distance = GlobalPosition.DistanceTo(((Node3D)player).GlobalPosition);
				if (distance < 1.5f) // Very close
				{
					// Optional auto-highlight for very close objects
					// Highlight(true);
				}
			}
		}
	}
}
