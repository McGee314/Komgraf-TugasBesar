using Godot;

public partial class AnimationCombine : CharacterBody3D
{
	// Movement Settings
	[Export] private float WalkSpeed = 5.0f;
	[Export] private float RunSpeed = 8.0f;
	[Export] private float JumpForce = 15.0f;
	[Export] private float Gravity = 30.0f;
	
	// Camera Settings
	[Export] private float MouseSensitivity = 0.005f;
	
	// Pickup Settings
	[Export] private float PickupRange = 3.0f;
	[Export] private string PickupInputAction = "pickup"; // Default "E" key
	
	// Camera variables
	private float cameraXRotation = 0.0f;
	private Node3D head;
	private Camera3D camera;
	
	// Animation variables
	private AnimationTree _animTree;
	private AnimationPlayer _animPlayer;
	private string _currentAnimState = "";
	private bool _isJumping = false;
	
	// Movement state
	private bool _isMoving = false;
	private bool _isRunning = false;
	
	// Pickup System - PERBAIKAN MAJOR
	private Area3D _pickupArea;
	private CollisionShape3D _pickupCollision;
	private Marker3D _handMarker;
	private Node3D _heldObject = null;
	private Node3D _highlightedObject = null;
	private Godot.Collections.Array<Node3D> _nearbyPickupables = new();
	
	public override void _Ready()
	{
		GD.Print("🎭 AnimationCombine loaded!");
		
		// Add to player group
		AddToGroup("Player");
		
		// Setup mouse capture with delay
		CallDeferred(nameof(SetupMouseCapture));
		
		// Setup components
		SetupCamera();
		SetupAnimations();
		SetupPickupSystem();
		
		PrintControlsHelp();
	}
	
	private void SetupMouseCapture()
	{
		Input.MouseMode = Input.MouseModeEnum.Captured;
		GD.Print("🖱️ Mouse captured!");
	}
	
	private void SetupCamera()
	{
		try
		{
			head = GetNode<Node3D>("Head");
			camera = GetNode<Camera3D>("Head/Camera3D");
			GD.Print("📷 Camera found!");
		}
		catch (System.Exception e)
		{
			GD.Print($"⚠️ Creating camera: {e.Message}");
			head = new Node3D();
			head.Name = "Head";
			AddChild(head);
			
			camera = new Camera3D();
			camera.Name = "Camera3D";
			head.AddChild(camera);
			GD.Print("📷 Camera created!");
		}
	}
	
	private void SetupAnimations()
	{
		try
		{
			_animPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
			GD.Print("✅ AnimationPlayer found");
		}
		catch { GD.Print("⚠️ No AnimationPlayer"); }
		
		try
		{
			_animTree = GetNode<AnimationTree>("AnimationTree");
			if (_animTree != null)
			{
				_animTree.Active = true;
				GD.Print("✅ AnimationTree active");
			}
		}
		catch { GD.Print("⚠️ No AnimationTree"); }
	}
	
	private void SetupPickupSystem()
	{
		// PERBAIKAN: Gunakan Area3D untuk deteksi pickup yang lebih reliable
		_pickupArea = new Area3D();
		_pickupArea.Name = "PickupArea";
		_pickupArea.Monitoring = true;
		_pickupArea.Monitorable = false;
		
		// Set collision layers untuk pickup detection
		_pickupArea.CollisionLayer = 0; // Player pickup area tidak perlu collision layer
		_pickupArea.CollisionMask = 2;  // Detect layer 2 (pickup items)
		
		AddChild(_pickupArea);
		
		// Buat CollisionShape3D untuk pickup area
		_pickupCollision = new CollisionShape3D();
		var sphere = new SphereShape3D();
		sphere.Radius = PickupRange;
		_pickupCollision.Shape = sphere;
		_pickupArea.AddChild(_pickupCollision);
		
		// Connect signals
		_pickupArea.BodyEntered += OnPickupableEntered;
		_pickupArea.BodyExited += OnPickupableExited;
		
		// Setup hand marker
		try
		{
			_handMarker = GetNode<Marker3D>("%Marker3D");
			if (_handMarker != null)
			{
				GD.Print("✅ Hand Marker3D found");
			}
		}
		catch 
		{ 
			GD.Print("⚠️ Creating Marker3D for hand");
			_handMarker = new Marker3D();
			_handMarker.Name = "HandMarker";
			_handMarker.Position = new Vector3(0.5f, 0, -0.8f);
			AddChild(_handMarker);
		}
		
		GD.Print("🎯 Pickup system setup complete!");
		GD.Print($"   PickupRange: {PickupRange}");
		GD.Print($"   CollisionMask: {_pickupArea.CollisionMask}");
	}
	
	private void OnPickupableEntered(Node3D body)
	{
		if (IsPickupableObject(body) && !_nearbyPickupables.Contains(body))
		{
			_nearbyPickupables.Add(body);
			GD.Print($"📦 Pickupable entered range: {body.Name}");
		}
	}
	
	private void OnPickupableExited(Node3D body)
	{
		if (_nearbyPickupables.Contains(body))
		{
			_nearbyPickupables.Remove(body);
			
			// Unhighlight if it was highlighted
			if (_highlightedObject == body)
			{
				UnhighlightObject(body);
				_highlightedObject = null;
			}
			
			GD.Print($"📦 Pickupable left range: {body.Name}");
		}
	}
	
	private bool IsPickupableObject(Node3D node)
	{
		// PERBAIKAN: Check multiple criteria
		bool isPickupable = node.IsInGroup("Pickupable") || 
						   node.HasMethod("CanBePickedUpMethod");
						   //node.Name.ToLower().Contains("indomie") ||
						   //node.Name.ToLower().Contains("ayam");
		
		if (isPickupable)
		{
			GD.Print($"✅ {node.Name} is pickupable!");
		}
		
		return isPickupable;
	}
	
	// PERBAIKAN MOUSE: Gunakan _Input untuk mouse
	public override void _Input(InputEvent @event)
	{
		// Handle mouse movement
		if (@event is InputEventMouseMotion mouseMotion && Input.MouseMode == Input.MouseModeEnum.Captured)
		{
			// Horizontal rotation - rotate character body
			RotateY(-mouseMotion.Relative.X * MouseSensitivity);
			
			// Vertical rotation - rotate head only
			if (head != null)
			{
				cameraXRotation -= mouseMotion.Relative.Y * MouseSensitivity;
				cameraXRotation = Mathf.Clamp(cameraXRotation, -1.4f, 1.4f); // -80 to 80 degrees
				head.Rotation = new Vector3(cameraXRotation, 0, 0);
			}
		}
		
		// Toggle mouse capture
		if (@event.IsActionPressed("ui_cancel"))
		{
			if (Input.MouseMode == Input.MouseModeEnum.Captured)
			{
				Input.MouseMode = Input.MouseModeEnum.Visible;
				GD.Print("🖱️ Mouse RELEASED");
			}
			else
			{
				Input.MouseMode = Input.MouseModeEnum.Captured;
				GD.Print("🖱️ Mouse CAPTURED");
			}
		}
		
		// Handle pickup input
		if (@event.IsActionPressed("pickup") || (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.E))
		{
			HandlePickupInput();
		}
	}
	
	// PERBAIKAN MOVEMENT: Simplifikasi physics process
	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;
		
		// Gravity
		if (!IsOnFloor())
			velocity.Y -= Gravity * (float)delta;
		
		// Jump
		if (IsOnFloor() && (Input.IsKeyPressed(Key.Space) || Input.IsActionJustPressed("jump")))
		{
			velocity.Y = JumpForce;
			TriggerJump();
		}
		
		// Movement - LANGSUNG TANPA INTERPOLASI DULU
		HandleDirectMovement(ref velocity);
		
		// Apply movement
		Velocity = velocity;
		MoveAndSlide();
		
		// Update pickup highlighting
		UpdatePickupHighlighting();
		
		// Update animations (kurangi frequency)
		if (Engine.GetProcessFrames() % 3 == 0) // Update setiap 3 frame
		{
			UpdateAnimations();
		}
	}
	
	private void HandleDirectMovement(ref Vector3 velocity)
	{
		Vector2 inputDir = Vector2.Zero;
		
		// Direct key input - paling reliable
		if (Input.IsKeyPressed(Key.W)) inputDir.Y -= 1;
		if (Input.IsKeyPressed(Key.S)) inputDir.Y += 1;
		if (Input.IsKeyPressed(Key.A)) inputDir.X -= 1;
		if (Input.IsKeyPressed(Key.D)) inputDir.X += 1;
		
		_isMoving = inputDir.Length() > 0;
		_isRunning = Input.IsKeyPressed(Key.Shift) && _isMoving;
		
		if (_isMoving)
		{
			// Normalize input
			inputDir = inputDir.Normalized();
			
			// Convert to 3D direction
			Vector3 direction = new Vector3(inputDir.X, 0, inputDir.Y);
			
			// Transform to world space
			direction = Transform.Basis * direction;
			
			// Set speed
			float speed = _isRunning ? RunSpeed : WalkSpeed;
			
			// Apply directly to velocity
			velocity.X = direction.X * speed;
			velocity.Z = direction.Z * speed;
		}
		else
		{
			// Stop horizontal movement
			velocity.X = 0;
			velocity.Z = 0;
		}
	}
	
	private void UpdatePickupHighlighting()
	{
		// Find closest pickupable object
		Node3D closestObject = null;
		float closestDistance = float.MaxValue;
		
		foreach (Node3D obj in _nearbyPickupables)
		{
			if (!IsInstanceValid(obj)) continue;
			
			// Check if object can be picked up
			bool canPickup = true;
			if (obj.HasMethod("CanBePickedUpMethod"))
			{
				canPickup = obj.Call("CanBePickedUpMethod").AsBool();
			}
			
			if (!canPickup) continue;
			
			float distance = GlobalPosition.DistanceTo(obj.GlobalPosition);
			if (distance < closestDistance)
			{
				closestDistance = distance;
				closestObject = obj;
			}
		}
		
		// Update highlighting
		if (_highlightedObject != closestObject)
		{
			// Unhighlight previous object
			UnhighlightObject(_highlightedObject);
			
			// Highlight new object
			_highlightedObject = closestObject;
			HighlightObject(_highlightedObject);
		}
	}
	
	private void HighlightObject(Node3D obj)
	{
		if (obj == null) return;
		
		if (obj.HasMethod("Highlight"))
		{
			obj.Call("Highlight", true);
		}
		
		GD.Print($"✨ Highlighting: {obj.Name}");
	}
	
	private void UnhighlightObject(Node3D obj)
	{
		if (obj == null) return;
		
		if (obj.HasMethod("Highlight"))
		{
			obj.Call("Highlight", false);
		}
	}
	
	private void HandlePickupInput()
	{
		if (_heldObject != null)
		{
			// Drop current object
			DropObject();
		}
		else if (_highlightedObject != null)
		{
			// Pick up highlighted object
			PickupObject(_highlightedObject);
		}
		else
		{
			GD.Print("❌ No object to pickup");
		}
	}
	
	private void PickupObject(Node3D obj)
	{
		if (obj == null || _handMarker == null) return;
		
		GD.Print($"🤏 Attempting to pickup: {obj.Name}");
		
		// Check if object can be picked up
		if (obj.HasMethod("CanBePickedUpMethod"))
		{
			bool canPickup = obj.Call("CanBePickedUpMethod").AsBool();
			if (!canPickup)
			{
				GD.Print($"❌ Cannot pickup {obj.Name}");
				return;
			}
		}
		
		// Store original data
		obj.SetMeta("original_parent", obj.GetParent());
		obj.SetMeta("original_transform", obj.Transform);
		
		// Remove from current parent
		var originalParent = obj.GetParent();
		originalParent.RemoveChild(obj);
		
		// Add to hand marker
		_handMarker.AddChild(obj);
		
		// Reset local transform
		obj.Position = Vector3.Zero;
		obj.Rotation = Vector3.Zero;
		obj.Scale = Vector3.One;
		
		// Disable collision
		if (obj.HasMethod("SetCollisionEnabled"))
		{
			obj.Call("SetCollisionEnabled", false);
		}
		
		// Call pickup method
		if (obj.HasMethod("OnPickedUp"))
		{
			obj.Call("OnPickedUp", this);
		}
		
		// Remove from nearby list
		if (_nearbyPickupables.Contains(obj))
		{
			_nearbyPickupables.Remove(obj);
		}
		
		_heldObject = obj;
		_highlightedObject = null;
		
		GD.Print($"✅ Successfully picked up: {obj.Name}");
	}
	
	private void DropObject()
	{
		if (_heldObject == null) return;
		
		GD.Print($"🗑️ Dropping: {_heldObject.Name}");
		
		// Get original parent
		var originalParent = _heldObject.GetMeta("original_parent").AsGodotObject() as Node;
		
		// Store current world transform
		var worldTransform = _heldObject.GlobalTransform;
		
		// Remove from hand
		_handMarker.RemoveChild(_heldObject);
		
		// Add back to original parent or scene
		if (originalParent != null && IsInstanceValid(originalParent))
		{
			originalParent.AddChild(_heldObject);
		}
		else
		{
			GetTree().CurrentScene.AddChild(_heldObject);
		}
		
		// Set drop position (in front of player)
		var dropPosition = GlobalPosition + (-Transform.Basis.Z * 2.0f) + Vector3.Up * 1.0f;
		_heldObject.GlobalPosition = dropPosition;
		
		// Re-enable collision
		if (_heldObject.HasMethod("SetCollisionEnabled"))
		{
			_heldObject.Call("SetCollisionEnabled", true);
		}
		
		// Call drop method
		if (_heldObject.HasMethod("OnDropped"))
		{
			_heldObject.Call("OnDropped", this);
		}
		
		// Clean up metadata
		_heldObject.RemoveMeta("original_parent");
		_heldObject.RemoveMeta("original_transform");
		
		GD.Print($"✅ Dropped: {_heldObject.Name}");
		_heldObject = null;
	}
	
	private void TriggerJump()
	{
		if (!_isJumping)
		{
			_isJumping = true;
			// Reset jump state setelah 1 detik
			GetTree().CreateTimer(1.0f).Timeout += () => { _isJumping = false; };
		}
	}
	
	private void UpdateAnimations()
	{
		string targetAnimation = "idle";
		
		if (_isJumping || !IsOnFloor())
			targetAnimation = "jump";
		else if (_isMoving && _isRunning)
			targetAnimation = "run";
		else if (_isMoving)
			targetAnimation = "walk";
		
		if (_currentAnimState != targetAnimation)
		{
			PlayAnimation(targetAnimation);
			_currentAnimState = targetAnimation;
		}
	}
	
	private void PlayAnimation(string animationName)
	{
		// Try AnimationTree first
		if (_animTree != null)
		{
			try
			{
				// Reset conditions
				_animTree.Set("parameters/conditions/idle", false);
				_animTree.Set("parameters/conditions/walk", false);
				_animTree.Set("parameters/conditions/run", false);
				_animTree.Set("parameters/conditions/jump", false);
				
				// Set target condition
				_animTree.Set($"parameters/conditions/{animationName}", true);
				return;
			}
			catch { }
		}
		
		// Fallback to AnimationPlayer
		if (_animPlayer != null && _animPlayer.HasAnimation(animationName))
		{
			_animPlayer.Play(animationName);
		}
	}
	
	private void PrintControlsHelp()
	{
		GD.Print("🎮 === PICKUP CONTROLS ===");
		GD.Print("  WASD: Move");
		GD.Print("  Shift: Sprint");
		GD.Print("  Space: Jump");
		GD.Print("  Mouse: Look around");
		GD.Print("  E: Pickup/Drop items");
		GD.Print("  ESC: Toggle mouse");
		GD.Print("========================");
	}
	
	// Public methods
	public bool IsHoldingObject() => _heldObject != null;
	public Node3D GetHeldObject() => _heldObject;
	public bool CanPickupObject() => _highlightedObject != null && _heldObject == null;
	public int GetNearbyPickupableCount() => _nearbyPickupables.Count;
}
