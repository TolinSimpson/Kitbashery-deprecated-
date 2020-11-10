bl_info = {
    "name": "Kitbashery Live Link",
    "description": "Live link between Blender and Kitbashery",
    "author": "Kitbashery",
    "version": (1, 0, 0),
    "blender": (2, 83, 0),
    #"location": "File > Import",
    "category": "Import-Export",
    "wiki_url": "Kitbashery.com"
}

import bpy, json, asyncio

port = 26738

async def listen(reader, writer):
    while True:
        data = await reader.read(8192)
            
        if not data:
            break
            
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
                                        
                #Weld vertices (This is a temp fix for a bug in Kitbashery's mesh combiner):
                bpy.ops.mesh.remove_doubles(threshold=0.0001, use_unselected=False)
                    
                #Convert from tris to quads:
                bpy.ops.mesh.tris_convert_to_quads(face_threshold=0.698132, shape_threshold=0.698132, uvs=False, vcols=False, seam=False, sharp=False, materials=False)
                    
                #Get seams from uv islands:
                bpy.ops.uv.seams_from_islands(mark_seams=True, mark_sharp=False)
               
            except:
                print("Kitbashery Live link: No data found?")
                break;
                    
def stopServer():
    try:
        for task in asyncio.Task.all_tasks():
            task.cancel()
    except:
        pass
            
    print("Kitbashery live link has stopped listening.")
           
async def startServer():
    print("Initialized Kitbashery live link... listening...")    
    server = await asyncio.start_server(listen, 'localhost', port)
    await server.serve_forever()
    #blender timers want a return value here but that never happens.
    
#https://docs.blender.org/api/current/bpy.app.timers.html
bpy.app.timers.register(startServer, first_interval=5, persistent=True)
    
#Blender addon stuff that is required but isn't really used:
class LiveLink(bpy.types.Operator):
    bl_idname = "live.link"
    bl_label = "Listen to Kitbashery"
      
def register():
    bpy.utils.register_class(LiveLink)

def unregister():
    bpy.utils.unregister_class(LiveLink)
    stopServer()

if __name__ == "__main__":
    register()
