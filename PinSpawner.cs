using Godot;

public class PinSpawner : Position3D
{
    [Export] private Vector3 _pinOffset;
    [Export] private int _maxRowLength;
    private PackedScene _pinScene = GD.Load<PackedScene>("res://Pin.tscn");
    private RigidBody[] _pins;

    public override void _Ready()
    {
        Translation = new Vector3(Translation.x, _pinOffset.y, Translation.z);
        SpawnPins();
    }

    private int CalculatePinSize()
    {
        int size = _maxRowLength;
        for (int i = _maxRowLength - 1; i > 0; i--) { size += i; }
        return size;
    }

    private void InstantiatePin(ref RigidBody pin, Vector3 position)
    {
        pin = (RigidBody)_pinScene.Instance();
        AddChild(pin);
        pin.Translation = position;
        pin.Mode = RigidBody.ModeEnum.Rigid;
    }

    private void SpawnPins()
    {
        int size = CalculatePinSize();
        int pinCount = _maxRowLength;
        Vector3 position = Vector3.Zero;
        _pins = new RigidBody[size--];
        while (size >= 0)
        {
            int left, mid = size - (pinCount / 2), right = mid + 1;
            if (pinCount % 2 != 0) //odd
            {
                InstantiatePin(ref _pins[size], position);
                left = mid - 1;
                position.x = _pinOffset.x;
            }
            else
            {
                left = mid;
                position.x = _pinOffset.x * 0.5f;
            }

            for (int i = 0; i <= (int)(pinCount * 0.5f) - 1; i++)
            {
                InstantiatePin(ref _pins[left - i], new Vector3(-position.x, position.y, position.z));
                InstantiatePin(ref _pins[right + i], position);
                position.x += _pinOffset.x;
            }
            position = new Vector3(0, 0, position.z + _pinOffset.z);
            size -= pinCount--;
        }
    }
}
