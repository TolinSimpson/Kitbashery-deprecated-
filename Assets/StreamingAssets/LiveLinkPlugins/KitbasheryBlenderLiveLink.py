bl_info = {
    "name": "Kitbashery Live Link",
    "description": "Live link between Blender and Kitbashery",
    "author": "Kitbashery",
    "version": (1, 0, 0),
    "blender": (2, 83, 0),
    "location": "",
    "category": "Import-Export",
    "wiki_url": "Kitbashery.com"
}

import bpy, sys, socket, json

host, port = 'localhost', 26738

class LiveLink():
    bl_idname = "live.link"

    def execute(self, context):
        client_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        client_socket.connect((host, port))
        while 1:
            data = client_socket.recv(512)
            if len(data) > 0:
                try:
                    #Deselect all selections:
                    context.selected_objects = null
                    
                    #import mesh from json:
                    jsonData = json.Loads(data.decode())
                    print(jsonData)
                    print(jsonData["meshPath"])
                    #https://docs.blender.org/api/current/bpy.ops.import_scene.html
                    bpy.ops.import_scene.obj(filepath= jsonData["meshPath"], filter_glob="*.obj;", use_edges=True, use_smooth_groups=True, use_split_objects=False, use_split_groups=False, use_groups_as_vgroups=False, use_image_search=False, split_mode='ON', global_clight_size=0.0, axis_forward='-Z', axis_up='Y')
                    
                    
                    #bpy.context.active_object
                    
                    #Weld vertices (This is a temp fix for a bug in Kitbashery's mesh combiner):
                    bpy.ops.mesh.remove_doubles(threshold=0.0001, use_unselected=False)
                    
                    #Convert from tris to quads:
                    bpy.ops.mesh.tris_convert_to_quads(face_threshold=0.698132, shape_threshold=0.698132, uvs=False, vcols=False, seam=False, sharp=False, materials=False)
                    
                    #Get seams from uv islands:
                    bpy.ops.uv.seams_from_islands(mark_seams=True, mark_sharp=False)
               
                except:
                    print("no data?")
                    #client_socket.close()
                    break;
                    
def register():
    bpy.utils.register_class(LiveLink)

def unregister():
    #client_socket.close()
    bpy.utils.unregister_class(LiveLink)

if __name__ == "__main__":
    register()
