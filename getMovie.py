import requests
import simplejson
import urllib

key = "wgcmq3renwa7nqecbbz2m8mf"
movieName = ""
img_url = ""
rtnData = {}

def getMovie(movie,info):
    global movieName
    global img_url
    global year
    global rtnData
    rtnData = {}

    movieEncode = movie.replace(" ","+")
    url = "http://api.rottentomatoes.com/api/public/v1.0/movies.json?q=%s&page_limit=1&page=1&apikey=" + key
    res = requests.get(url % movieEncode)
 
    data = res.content
 
    js = simplejson.loads(data)
    movies = js['movies']
    
    if(js['total']>0 and movies[0]["title"].lower() == movie.lower()):
        movieName = movies[0]["title"]
        img_url = movies[0]['posters']['detailed']
        for inf in info:
            rtnData[inf] = movies[0][inf]
    else:
        print ("Movie not found")

user = raw_input("Enter in the exact movie name: ")
info = raw_input("What information do you want?")
info = info.split()
getMovie(user,info)
