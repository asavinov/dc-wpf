
//
// xmlns:cmd="clr-namespace:Commands"
//
// <Button Command="cmd:DataCommands.Requery">Requery</Button>
/*
<Window.CommandBindings> 
<CommandBinding Command="cmd:DataCommands.Requery" Executed="RequeryCommand_Executed" CanExecute="RequeryCommand_CanExecute"></CommandBinding> 
</Window.CommandBindings>
*/


public /* static */ class DataCommands 
{ 

private static RoutedUICommand requery;

public static readonly RoutedUICommand SomeOtherAction = new RoutedUICommand("Some other action", "SomeOtherAction", typeof(Window1));

static DataCommands() 
{ 
// Initialize the command. 
InputGestureCollection inputs = new InputGestureCollection(); 
inputs.Add(new KeyGesture(Key.R, ModifierKeys.Control, "Ctrl+R")); 

requery = new RoutedUICommand("Requery", "Requery", typeof(DataCommands), inputs); 

} 
public static RoutedUICommand Requery 
{ 
get { return requery; } 
} 
}
