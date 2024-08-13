class_name DialogueBox

static var box_pos_presets: Dictionary = {
	bottom = {
		box = "default_box",
		anchor = [0.1, 0.9, 0.65, 0.95],
	},
	top = {
		box = 'default_box',
		anchor = [0.1, 0.9, 0.05, 0.35],
	},
	center = {
		box = 'default_box',
		anchor = [0.1, 0.9, 0.35, 0.65],
	},
	left = {
		box = 'basic_box',
		anchor = [0, 0.5, 0, 1],
	},
	right = {
		box = 'basic_box',
		anchor = [0.5, 1, 0, 1],
	},
	full = {
		box = 'basic_box',
		anchor = [0, 1, 0, 1],
	},
	hide = {
		box = null,
	},
}

#@export
static func set_box(pos_name="bottom"):
	var pos = box_pos_presets[pos_name];

	var box = BuiltIn.o[pos.box] if pos.get("box") != null else null

	if box != null:
		var anchor = pos.get("anchor", [0, 1, 0, 1])
		box.anchor_left = anchor[0]
		box.anchor_right = anchor[1]
		box.anchor_top = anchor[2]
		box.anchor_bottom = anchor[3]

		var offset = pos.get("offset", [0, 0, 0, 0])
		box.offset_left = offset[0]
		box.offset_right = offset[1]
		box.offset_top = offset[2]
		box.offset_bottom = offset[3]

	BuiltIn._nova.GameViewController.SwitchDialogueBox(box, true)

static var set_box_l = set_box
