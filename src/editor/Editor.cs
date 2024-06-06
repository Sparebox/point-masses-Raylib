using Sim;

namespace Editing;

public class Editor
{
    private Context _context;
    private Grid _grid;

    public Editor(Context context)
    {
        _context = context;
        _grid = new Grid(5);
    }

    public void Draw()
    {
        _grid.Draw();
    }

    public void Update()
    {
        
    }

}