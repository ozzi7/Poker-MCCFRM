from dask_ml import cluster
import numpy as np

if __name__ == '__main__':
    X = np.random.rand(1000000, 16)
    kmeans = cluster.PartialMiniBatchKMeans(n_clusters=500, init='k-means++').partial_fit(X)
    print(kmeans.labels_)
