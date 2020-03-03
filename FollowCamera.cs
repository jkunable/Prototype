using Godot;

public class FollowCamera : Camera
{
    [Export] private Vector3 _cameraOffset = new Vector3(0, 6, 7.5f);
    [Export] private float _rotationOffset = 16;
    private Spatial _target;
    [Export] private float _stopPoint;

    public override void _Ready()
    {

        _target = (Spatial)Owner;
        Translation = _target.Translation + _cameraOffset;//position camera
        LookAt(_target.Translation, Vector3.Up);
        RotationDegrees = new Vector3(RotationDegrees.x + _rotationOffset, RotationDegrees.y, RotationDegrees.z);
        Current = true;//remove
        SetProcess(false);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta)
    {
        Vector3 pos = _cameraOffset;
        //1 represents the object above ground height//may be caluculated in ready in the future
        pos.y = Mathf.Clamp(pos.y + _target.Translation.y, _cameraOffset.y + 1, _cameraOffset.y * 2);
        //Mathf.Clamp()
        pos.z = (_cameraOffset.z - _target.Translation.z <= _cameraOffset.z) ? _cameraOffset.z : _target.Translation.z + _cameraOffset.z;
        if (pos.z <= _stopPoint)
        {
            pos.z = _stopPoint;
            SetProcess(false);
        }

        Translation = pos;
    }
}