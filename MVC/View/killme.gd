extends Node3D


# Called when the node enters the scene tree for the first time.
func _ready():
	SwitchToTopDown()


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	pass

func SwitchToTopDown():
	var tween = get_tree().create_tween()
	tween.set_parallel(true);
	tween.tween_property(self, "rotation_degrees", Vector3(-90, 0, 0), 1).set_ease(Tween.EASE_IN)
	tween.tween_property(self, "position", Vector3(0, 1.8, 0), 1).set_ease(Tween.EASE_IN)

func SwitchToPerspective():
	var tween = get_tree().create_tween()
	tween.set_parallel(true);
	tween.tween_property(self, "rotation_degrees", Vector3(-90, 0, 0), 1).set_ease(Tween.EASE_IN)
	tween.tween_property(self, "position", Vector3(0, 1.8, 0), 1).set_ease(Tween.EASE_IN)
