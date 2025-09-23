import pandas as pd
import numpy as np
import hdbscan
import sys
import os
import simplekml

# --- Constants --- 

MIN_CLUSTER_SIZE = 3 # Defined as a constant at the top

#This code requires the following packages:
# pandas, numpy, hdbscan, simplekml

def create_points_array_from_csv(file_path: str):
    """
    Reads a CSV file, filters for points of Type 'Sector',
    and returns their Latitude and Longitude as a NumPy array.
    Also returns the filtered DataFrame for later use.
    """
    try:
        df = pd.read_csv(file_path)
        if 'CellID' in df.columns:
            df['CellID'] = df['CellID'].astype(str)
        
        sectors_df = df[df['Type'] == 'Sector'].copy()
        points = sectors_df[['Latitude', 'Longitude']].to_numpy()
        # Return only the sectors_df, not the full one
        return points, sectors_df
    except FileNotFoundError:
        print(f"Error: The file '{file_path}' was not found.")
        sys.exit(1)
    except KeyError as e:
        print(f"Error: A required column is missing from the CSV file: {e}")
        sys.exit(1)

def map_cluster_kml(dataframe, output_filename="Python_kml_map.kml"):
    """
    Generates a KML file from the clustered data.
    - Sectors are marked with red balloons.
    - Cluster centroids are marked with light green balloons.
    """
    kml = simplekml.Kml()
    
    red_style = simplekml.Style()
    red_style.iconstyle.icon.href = 'http://maps.google.com/mapfiles/ms/icons/red-dot.png'
    
    green_style = simplekml.Style()
    green_style.iconstyle.icon.href = 'http://maps.google.com/mapfiles/ms/icons/green-dot.png'

    for index, row in dataframe.iterrows():
        pnt = kml.newpoint()
        pnt.coords = [(row['Longitude'], row['Latitude'])]

        if row['Type'] == 'Sector':
            pnt.name = str(row['CellID'])
            pnt.description = f"Type: Sector\nCluster ID: {row['cluster']}"
            pnt.style = red_style
        elif row['Type'] == 'Cluster_entry':
            pnt.name = f"Centroid for Cluster {row['cluster']}"
            pnt.description = f"Member CellIDs:\n{row['CellID']}"
            pnt.style = green_style

    kml.save(output_filename)
    print(f"\nSuccessfully generated KML map: '{output_filename}'")


def run_hdbscan_clustering(input_csv, output_csv, kml_filename = "Python_kml_map.kml"):
    
    # --- 1. Load and Prepare Data ---
    #input_csv = 'ALL_map_mnc_260.csv'
    points, sectors_df = create_points_array_from_csv(input_csv)

    if points.size == 0:
        print("No points of Type 'Sector' found. Exiting.")
        sys.exit(0)
    
    import numpy as np
    import hdbscan
    import os

    print(f"Successfully loaded {points.shape[0]} sector points.")
    radians_points = np.radians(points)

    # --- 2. Run the Clustering ---
    # Using the constant defined at the top of the script
    clusterer = hdbscan.HDBSCAN(min_cluster_size=MIN_CLUSTER_SIZE, metric='haversine')
    clusterer.fit(radians_points)
    sectors_df['cluster'] = clusterer.labels_

    # --- 3. Calculate Centroids and Format Data ---
    clusters_only_df = sectors_df[sectors_df['cluster'] != -1].copy()
    
    if clusters_only_df.empty:
        print("No clusters were formed. Try lowering the 'MIN_CLUSTER_SIZE' parameter.")
        # NEW: Save the file even if no clusters, just will show noise points
        sectors_df.to_csv(f"cluster_{os.path.basename(input_csv)}", index=False)
        sys.exit(0)
    
    cluster_counts = clusters_only_df['cluster'].value_counts()
    num_clusters = len(cluster_counts)
    print(f"\nIdentified {num_clusters} clusters (excluding noise).")

    centroids = clusters_only_df.groupby('cluster').agg(
        Latitude=('Latitude', 'mean'),
        Longitude=('Longitude', 'mean'),
        CellID=('CellID', lambda x: '/'.join(x))
    ).reset_index()
    centroids['Type'] = 'Cluster_entry'
    centroids['cluster'] = centroids['cluster'].apply(
        lambda x: f"{x} ({cluster_counts[x]} points)"
    )

    # --- 4. Prepare, Save, and Map Final Data ---
    base_name = os.path.basename(input_csv)
    output_csv = f"Python_cluster_{base_name}"
    
    # NEW: The final DataFrame now only contains the sectors and the new centroids
    final_df = pd.concat([sectors_df, centroids], ignore_index=True)

    final_df.to_csv(output_csv, index=False)
    print(f"Successfully saved results to '{output_csv}'")
    
    map_cluster_kml(final_df)

    if __name__ == "__main__":
        if len(sys.argv) < 3:
            print("Usage: python cluster_hdbscan.py <input_csv> <output_csv> [kml_filename]")
            sys.exit(1)
        input_csv = sys.argv[1]
        output_csv = sys.argv[2]
        kml_filename = sys.argv[3] if len(sys.argv) > 3 else "Python_kml_map.kml"
        print(f"Running HDBSCAN clustering on '{input_csv}'...")
        run_hdbscan_clustering(input_csv, output_csv, kml_filename)