using Physics;
using Raylib_cs;
using Sim;
using Utils;
using static Raylib_cs.Raylib;

namespace Editing;

public class Editor
{
    public float CursorRadius { get; set; }

    private readonly Context _context;
    private readonly Grid _grid;
    private readonly LinkedList<uint> _selectedPointIndices;

    public Editor(Context context)
    {
        _context = context;
        _grid = new Grid(5, this);
        _selectedPointIndices = new();
        CursorRadius = 10f;
    }

    public void Draw()
    {
        _grid.Draw();
    }

    public void Update()
    {
        if (IsMouseButtonDown(MouseButton.Left))
        {
            try {
                var mousePos = GetMousePosition();
                ref var closestGridPoint = ref _grid.GetClosestGridPoint((uint) mousePos.X, (uint) mousePos.Y);
                closestGridPoint.IsSelected = true;
                uint gridIndex = _grid.GetIndexFromPixel((uint) mousePos.X, (uint) mousePos.Y);
                if (!_selectedPointIndices.Contains(gridIndex))
                {
                    _selectedPointIndices.AddLast(gridIndex);
                }
            }
            catch (IndexOutOfRangeException)
            {
                Console.Error.WriteLine("Grid index was out of range");
            }
        }
        else
        {
            _grid.ResetSelectedPoints();
        }
        if (IsMouseButtonReleased(MouseButton.Left)) // Execute editor action
        {
            var particles = CreateParticlesFromIndices();
            _context.AddMassShapes(particles);
            _selectedPointIndices.Clear();
        }
    }

    private MassShape[] CreateParticlesFromIndices()
    {
        var particles = new List<MassShape>(_selectedPointIndices.Count);
        foreach (var index in _selectedPointIndices)
        {
            var gridPoint = _grid.GridPoints[index];
            particles.Add(MassShape.Particle(
                UnitConv.MetersToPixels(gridPoint.Pos.X),
                UnitConv.MetersToPixels(gridPoint.Pos.Y),
                PointMass.RadiusToMass(CursorRadius),
                _context
            ));
        }
        return particles.ToArray();   
    }

}