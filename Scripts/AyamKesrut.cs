using Godot;

// PERBAIKAN: Ganti nama class dari Ayamkesrut ke AyamKesrut (sesuai nama file)
public partial class AyamKesrut : CharacterBody3D
{
	// Pickup Settings
	[Export] private bool CanBePickedUp = true;
	[Export] private string ItemName = "Ayam Kesrut";
	[Export] private string ItemDescription = "Ayam yang sudah diolah dengan bumbu kesrut";
	
	// Highlight Settings
	[Export] private Color HighlightColor = Colors.Orange;
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
		GD.Print($"🍗 {ItemName} loaded!");
		
		// PENTING: Add to pickupable group
		AddToGroup("Pickupable");
		
		// Debug: Print all groups this node belongs to
		var groups = GetGroups();
		GD.Print($"📋 {ItemName} is in groups: {string.Join(", ", groups)}");
		
		// Setup components
		SetupComponents();
		SetupHighlightMaterial();
		SetupPhysics();
		
		GD.Print($"✅ {ItemName} ready for pickup!");
		
		// TAMBAHAN: Debug print node structure
		PrintNodeStructure();
	}
	
	private void PrintNodeStructure()
	{
		GD.Print($"🏗️ {ItemName} Node Structure:");
		GD.Print($"  - Root: {Name} ({GetType().Name})");
		foreach (Node child in GetChildren())
		{
			GD.Print($"  - Child: {child.Name} ({child.GetType().Name})");
			foreach (Node grandChild in child.GetChildren())
			{
				GD.Print($"    - Grandchild: {grandChild.Name} ({grandChild.GetType().Name})");
			}
		}
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
		
		// Find CollisionShape3D - COBA BEBERAPA CARA
		_collisionShape = GetNodeOrNull<CollisionShape3D>("CollisionShape3D");
		if (_collisionShape == null)
		{
			// Cari di children
			_collisionShape = FindChild("CollisionShape3D") as CollisionShape3D;
		}
		
		if (_collisionShape != null)
		{
			GD.Print($"✅ CollisionShape3D found: {_collisionShape.Name}");
			GD.Print($"   Shape: {_collisionShape.Shape}");
			GD.Print($"   Disabled: {_collisionShape.Disabled}");
		}
		else
		{
			GD.Print("❌ CollisionShape3D NOT found!");
			// Buat CollisionShape3D otomatis jika tidak ada
			CreateCollisionShape();
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
	
	private void CreateCollisionShape()
	{
		GD.Print("🔧 Creating CollisionShape3D automatically...");
		
		_collisionShape = new CollisionShape3D();
		_collisionShape.Name = "CollisionShape3D";
		
		// Buat shape box sederhana
		var boxShape = new BoxShape3D();
		boxShape.Size = new Vector3(1, 1, 1); // Sesuaikan ukuran
		_collisionShape.Shape = boxShape;
		
		AddChild(_collisionShape);
		GD.Print("✅ CollisionShape3D created successfully!");
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
		_highlightMaterial.Roughness = 0.3f;
		_highlightMaterial.Metallic = 0.1f;
	}
	
	private void SetupPhysics()
	{
		// Set collision layers - PENTING untuk detection
		CollisionLayer = 2; // Layer 2 for pickup items
		CollisionMask = 1;  // Can collide with world (layer 1)
		
		GD.Print($"🔧 {ItemName} physics setup:");
		GD.Print($"   CollisionLayer: {CollisionLayer}");
		GD.Print($"   CollisionMask: {CollisionMask}");
		GD.Print($"   GlobalPosition: {GlobalPosition}");
	}
	
	// PERBAIKAN: Method yang dipanggil pickup system
	public bool CanBePickedUpMethod()
	{
		bool canPickup = CanBePickedUp && !_isPickedUp;
		GD.Print($"🤔 {ItemName} can be picked up: {canPickup}");
		return canPickup;
	}
	
	// Method called by pickup system for highlighting
	public void Highlight(bool enable)
	{
		if (_meshInstance == null) 
		{
			GD.Print($"⚠️ Cannot highlight {ItemName} - no mesh instance");
			return;
		}
		
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
		
		// Set posisi sedikit ke atas saat di-drop
		if (IsInstanceValid(this))
		{
			GlobalPosition += Vector3.Up * 0.1f;
			Velocity = Vector3.Up * 2.0f;
		}
	}
	
	// Method to enable/disable collision (called by pickup system)
	public void SetCollisionEnabled(bool enabled)
	{
		if (_collisionShape != null)
		{
			_collisionShape.Disabled = !enabled;
			GD.Print($"🔧 {ItemName} collision shape disabled: {!enabled}");
		}
		
		// Also set collision layers
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
		
		GD.Print($"🔧 {ItemName} collision enabled: {enabled}");
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
		// Simple particle effect
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
	
	// Method to reset object state
	public void ResetObject()
	{
		_isPickedUp = false;
		_currentHolder = null;
		Highlight(false);
		SetCollisionEnabled(true);
		Velocity = Vector3.Zero;
		
		GD.Print($"🔄 {ItemName} reset");
	}
	
	// ENHANCED DEBUG: Print status lebih sering untuk troubleshooting
	public override void _PhysicsProcess(double delta)
	{
		// Debug: Print status setiap 2 detik saat testing
		if (Engine.GetProcessFrames() % 120 == 0) // Setiap 2 detik (60fps * 2)
		{
			GD.Print($"🍗 {ItemName} Status:");
			GD.Print($"   Position: {GlobalPosition}");
			GD.Print($"   InGroup Pickupable: {IsInGroup("Pickupable")}");
			GD.Print($"   CollisionLayer: {CollisionLayer}");
			GD.Print($"   CollisionMask: {CollisionMask}");
			GD.Print($"   Can be picked up: {CanBePickedUpMethod()}");
			
			if (_collisionShape != null)
			{
				GD.Print($"   Collision disabled: {_collisionShape.Disabled}");
			}
		}
	}
}
