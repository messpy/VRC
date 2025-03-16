import bpy
import math

#blender --background --python script.py


text = input("text:")
# ※ オプション：シーン内の既存オブジェクトを削除してクリーンな状態にする
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

# 1. 文字生成と3D化
font_curve = bpy.data.curves.new(type="FONT", name="MyTextCurve")
font_curve.body = text   # 生成する文字を指定
font_curve.extrude = 0.1  # 3D化のための厚み（extrude値）を設定

# 文字オブジェクトの作成とシーンへの追加
text_obj = bpy.data.objects.new(name="My3DTextObject", object_data=font_curve)
bpy.context.scene.collection.objects.link(text_obj)

# 2. マテリアルの作成と適用
mat = bpy.data.materials.new(name="MyTextMaterial")
mat.diffuse_color = (1, 0, 0, 1)  # 赤色（RGBA）を設定
if text_obj.data.materials:
    text_obj.data.materials[0] = mat
else:
    text_obj.data.materials.append(mat)

# 3. オブジェクトに回転を加える（例：X=30°, Y=45°, Z=60°）
#text_obj.rotation_euler = (math.radians(30), math.radians(45), math.radians(60))

# 4. FBX形式でエクスポート
# エクスポート対象のオブジェクトを選択
bpy.ops.object.select_all(action='DESELECT')
text_obj.select_set(True)
bpy.context.view_layer.objects.active = text_obj

# 出力ファイルのパスを設定（適宜変更してください）
filepath = r"C:\Users\kenny\Documents" + "\\" + text+ ".fbx"
bpy.ops.export_scene.fbx(filepath=filepath, use_selection=True, axis_forward='-Z', axis_up='Y')

print("FBXファイルとしてエクスポートしました:", filepath)
