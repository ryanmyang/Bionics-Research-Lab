import json
import matplotlib.pyplot as plt
from mpl_toolkits.mplot3d import Axes3D
import numpy as np

# JSON data
with open('./11_19_22_29_touched_sphere_coords.json') as cartesian_json_file:
    cartesian_data = json.load(cartesian_json_file)

# Extract vectorList from the data
cartesian_vector_list = cartesian_data['vectorList']

# Separate x, y, and z coordinates
x = np.array([vector['x'] for vector in cartesian_vector_list])
y = np.array([vector['y'] for vector in cartesian_vector_list])
z = np.array([vector['z'] for vector in cartesian_vector_list])

# Define overall size and step size
overall_size = 0.8
step_size = 0.1

# Adjust axis limits
axis_limits = (-overall_size, overall_size)

# Create 3D scatter plot



# Load JSON data
with open('./11_19_23_43_touched_sphere_polar.json') as spherical_json_file:
    spherical_data = json.load(spherical_json_file)

# Extract vectorList from the data
spherical_vector_list = spherical_data['vectorList']

# Convert spherical coordinates to Cartesian coordinates
radius = np.array([vector['x'] for vector in spherical_vector_list])
polar_angle = np.array([vector['y'] for vector in spherical_vector_list])
elevation_angle = np.array([vector['z'] for vector in spherical_vector_list])

# Convert spherical to Cartesian coordinates
x_s = radius * np.sin(elevation_angle) * np.cos(polar_angle)
y_s = radius * np.sin(elevation_angle) * np.sin(polar_angle)
z_s = radius * np.cos(elevation_angle)

# Adjust axis limits
axis_limits = (-overall_size, overall_size)

# Create 3D scatter plot
fig_c = plt.figure()

ax = fig_c.add_subplot(111, projection='3d')
ax.scatter(x, z, y, c='b', marker='o')

# Set axis limits
ax.set_xlim(axis_limits)
ax.set_ylim(axis_limits)
ax.set_zlim(axis_limits)

# Set labels
ax.set_xlabel('X Axis')
ax.set_ylabel('Y Axis')
ax.set_zlabel('Z Axis')

fig_s = plt.figure()
ax_s = fig_s.add_subplot(111, projection='3d')
ax_s.scatter(x_s, y_s, z_s, c='b', marker='o')

# Set axis limits
ax_s.set_xlim(axis_limits)
ax_s.set_ylim(axis_limits)
ax_s.set_zlim(axis_limits)

# Set labels
ax_s.set_xlabel('X Axis')
ax_s.set_ylabel('Y Axis')
ax_s.set_zlabel('Z Axis')

plt.show()
