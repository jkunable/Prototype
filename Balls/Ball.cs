using Godot;

public class Ball : RigidBody
{
    private float _maxShootForce; //should be const value when values finalized
    [Export] protected float shootForce;
    protected Vector3 turnForce = Vector3.Zero;
    //start touch variable
    protected State state;
    protected State idleState, positionState, aimingState, rollingState;
    protected Camera camera;
    protected int touchIndex;
    protected Vector2 touchPosition;
    public override void _Ready()
    {
        _maxShootForce = shootForce;
        DefineStates();//must be done first
        SetState(idleState.GetType());
        //Attach camera
        camera = GetNode<Camera>("Container/ControlCamera");
    }

    public override void _UnhandledInput(InputEvent @event) { state.Input(@event); }
    public override void _PhysicsProcess(float delta) { state.Update(delta); }

    protected virtual void DefineStates()
    {
        idleState = new IdleState();
        positionState = new PositionState();
        aimingState = new AimingState();
        rollingState = new RollingState();
    }
    protected void SetState(System.Type type)
    {
        if (type == idleState.GetType()) { state = idleState; }
        else if (type == positionState.GetType()) { state = positionState; }
        else if (type == aimingState.GetType()) { state = aimingState; }
        else { state = rollingState; }
        state.Init(this);
    }

    public void FakeTouch(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.ButtonIndex == (int)ButtonList.Right)
            {
                InputEventScreenTouch ev = new InputEventScreenTouch();
                ev.Index = 1;
                ev.Position = (mouseButton.Position / 4);
                ev.Pressed = mouseButton.IsPressed();
                Godot.Input.ParseInputEvent(ev);
                GD.Print("FAKE");
            }
        }
    }

    /*public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_accept"))
        {
            ApplyImpulse(Vector3.Zero, new Vector3(0, 20, -_shootForce));
            AddTorque((Vector3.Up + Vector3.Right) * 250);
        }
        if (@event.IsActionPressed("ui_cancel"))
        {
            ApplyImpulse(Vector3.Zero, new Vector3(0, 0, _shootForce));
        }

        if (@event.IsActionPressed("ui_left"))
        {
            _turnForce.x = -40;
        }
        if (@event.IsActionPressed("ui_right"))
        {
            _turnForce.x = 40;
        }

        if (@event.IsActionReleased("ui_left"))
        {
            _turnForce.x = 0;
        }
        if (@event.IsActionReleased("ui_right"))
        {
            _turnForce.x = 0;
        }


        if (@event.IsActionPressed("ui_end"))
        {
            GetTree().ReloadCurrentScene();
        }
    }*/

    //--------------------------------------------------State Machine Class Structure----------------------------------------------------------------------//
    //--------------------------------------------------State Machine Class Structure----------------------------------------------------------------------//
    //--------------------------------------------------State Machine Class Structure----------------------------------------------------------------------//
    //state:BaseState//================================================================================================================================//
    protected class State
    {
        public virtual void Init(Ball b) { } //initalizes state :: use like a constructor
        public virtual void Input(InputEvent @event) { } //handles input events :: if input pressed do something
        public virtual void Update(float delta) { } //perform some action(s) or check(s) each frame
    }
    //state:IdleState//================================================================================================================================//
    //state:IdleState//================================================================================================================================//
    protected class IdleState : State
    {
        private Ball _ball; //reference to object
        private const int RAY_LENGTH = 1000;
        private const int NO_TOUCH = -1;
        public override void Init(Ball b)
        {
            _ball = b;
            _ball.touchIndex = NO_TOUCH;
            _ball.Mode = ModeEnum.Static;
            _ball.SetPhysicsProcess(false);
            GD.Print("IdleState");
        }

        public override void Input(InputEvent @event)
        {
            //-----------------------------------------------------
            _ball.FakeTouch(@event);
            //-----------------------------------------------------
            if (@event is InputEventScreenTouch screenTouch)
            {
                if (_ball.touchIndex == NO_TOUCH && screenTouch.IsPressed())
                {
                    _ball.touchIndex = screenTouch.Index;
                    _ball.touchPosition = screenTouch.Position;
                    _ball.SetPhysicsProcess(true);
                }
            }
        }

        public override void Update(float delta)
        {
            Vector3 from = _ball.camera.ProjectRayOrigin(_ball.touchPosition);
            Vector3 to = from + _ball.camera.ProjectRayNormal(_ball.touchPosition) * RAY_LENGTH;
            //cast ray
            Godot.Collections.Dictionary result = _ball.GetWorld().DirectSpaceState.IntersectRay(from,
             to, new Godot.Collections.Array { }, _ball.CollisionLayer);
            if (result.Count > 0)
            {
                _ball.SetState(_ball.positionState.GetType());//change state
            }
            else
            {
                _ball.touchIndex = NO_TOUCH; //didn't select the ball
                _ball.SetPhysicsProcess(false);
            }
        }
    }
    //state:PositionState//================================================================================================================================//
    //state:PositionState//================================================================================================================================//
    protected class PositionState : State
    {
        private const int LANE_WIDTH = 3; //this amount would come from an external class
        private Ball _ball; //reference to object
        private const float MAX_DRAG = 0.7f;
        private Vector3 _from, _initialPosition;
        public override void Init(Ball b)
        {
            _ball = b;
            _initialPosition = _ball.Translation;
            //if camera moves during this state, then _from should be calculated in update
            _from = _ball.camera.ProjectRayOrigin(_ball.touchPosition);
            GD.Print("PositionState");
        }

        public override void Input(InputEvent @event)
        {
            //-----------------------------------------------------
            _ball.FakeTouch(@event);
            //-----------------------------------------------------

            if (@event is InputEventScreenTouch screenTouch)
            {
                if (screenTouch.Index == _ball.touchIndex && !screenTouch.IsPressed())
                {
                    _ball.SetState(_ball.idleState.GetType());//change state
                }
            }
            if (@event is InputEventScreenDrag screenDrag && screenDrag.Index == _ball.touchIndex)
            {
                _ball.touchPosition = screenDrag.Position;
            }
        }

        public override void Update(float delta)
        {
            Vector3 to = _from + _ball.camera.ProjectRayNormal(_ball.touchPosition) * _from.DistanceTo(_ball.Translation); //raylength == _from.distanceto(ball)
            to.x = Mathf.Clamp(to.x, -LANE_WIDTH, LANE_WIDTH);//clamp x to size of bowling lane::which should be centered in the midddle
            _ball.Translation = new Vector3(to.x, _initialPosition.y, _initialPosition.z);
            if ((to.z - _initialPosition.z) > MAX_DRAG)
            {
                _ball.SetState(_ball.aimingState.GetType());//change state
            }
        }
    }
    //state:AimingState//================================================================================================================================//
    //state:AimingState//================================================================================================================================//
    protected class AimingState : State
    {
        private Ball _ball;
        private const float RETRACT_AMOUNT = 1.5f;
        private const float CANCEL_DISTANCE = 0.35f;
        private Vector3 _from, _initialPosition;
        private float _percentage;
        public override void Init(Ball b)
        {
            _ball = b;
            _initialPosition = _ball.Translation;
            _from = _ball.camera.ProjectRayOrigin(_ball.touchPosition);
            GD.Print("AimingState");
        }
        public override void Input(InputEvent @event)
        {
            //-----------------------------------------------------
            _ball.FakeTouch(@event);
            //-----------------------------------------------------
            if (@event is InputEventScreenTouch screenTouch)
            {
                if (screenTouch.Index == _ball.touchIndex && !screenTouch.IsPressed())
                {
                    if (Mathf.Abs(_ball.Translation.z - _initialPosition.z) <= CANCEL_DISTANCE)
                    {
                        _ball.Translation = _initialPosition;
                        //change state back to idle
                        _ball.SetState(_ball.idleState.GetType());
                    }
                    else
                    {
                        //calculate force and switch state to rolling state :: try to get the lerp percentage
                        _ball.shootForce = _ball._maxShootForce * _percentage;
                        _ball.SetState(_ball.rollingState.GetType());
                    }
                }
            }
            if (@event is InputEventScreenDrag screenDrag && screenDrag.Index == _ball.touchIndex)
            {
                _ball.touchPosition = screenDrag.Position;
            }
        }

        public override void Update(float delta)
        {
            Vector3 to = _from + _ball.camera.ProjectRayNormal(_ball.touchPosition) * _from.DistanceTo(_ball.Translation); //raylength == _from.distanceto(ball)

            _percentage = ((to.z - _initialPosition.z) / RETRACT_AMOUNT);
            _percentage = Mathf.Clamp(_percentage, 0, 1);//clamp percentage between 0 and 1

            float z = _initialPosition.z + (_percentage * RETRACT_AMOUNT);
            float zee = Mathf.Lerp(_initialPosition.z, z, 0.51f);

            _ball.Translation = new Vector3(to.x, _initialPosition.y, zee);
        }
    }
    //state:RollingState//================================================================================================================================//
    //state:RollingState//================================================================================================================================//
    protected class RollingState : State
    {
        private Ball _ball;
        public override void Init(Ball b)
        {
            _ball = b;
            _ball.Mode = ModeEnum.Rigid;
            _ball.ApplyImpulse(Vector3.Zero, new Vector3(0, 0, -_ball.shootForce));
            _ball.camera.SetProcess(true);
            GD.Print("RollingState: force = " + _ball.shootForce);
        }

        public override void Input(InputEvent @event)
        {
            if (@event.IsActionPressed("ui_left"))
            {
                _ball.turnForce.x = -40;
            }
            if (@event.IsActionPressed("ui_right"))
            {
                _ball.turnForce.x = 40;
            }

            if (@event.IsActionReleased("ui_left"))
            {
                _ball.turnForce.x = 0;
            }
            if (@event.IsActionReleased("ui_right"))
            {
                _ball.turnForce.x = 0;
            }

            //Debug::restart
            if (@event.IsActionPressed("ui_end"))
            {
                _ball.GetTree().ReloadCurrentScene();
            }
        }

        public override void Update(float delta)
        {
            if (_ball.turnForce != Vector3.Zero)
                _ball.AddForce(_ball.turnForce, Vector3.Zero);
        }
    }
}