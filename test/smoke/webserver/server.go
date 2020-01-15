package main

// !! CRITICAL SECURITY NOTICE !!
// WARNING: THIS SERVICE ENABLES ARBITRARY SHELL EXECUTION ON THE SERVER FOR REMOTE TEST EXECUTION
// MUST NEVER BE EXPOSED WITHOUT RESTRICTED INBOUNDS IPs

import (
	"encoding/json"
	"log"
	"net/http"
	"os/exec"
	"strings"

	"github.com/bitly/go-simplejson"
	"github.com/gorilla/mux"
)

type App struct {
	Router *mux.Router
}

type Command struct {
	Cmd  string `json:"cmd"`
	Args string `json:"args"`
	Dir  string `json:"dir"`
	Key  string `json:"key"`
}

func (app *App) setupRouter() {
	app.Router.
		Methods("POST").
		Path("/test").
		HandlerFunc(app.postCommand)

	app.Router.
		Methods("GET").
		Path("/").
		HandlerFunc(app.getAlive)
}

func (app *App) getAlive(w http.ResponseWriter, r *http.Request) {
	w.WriteHeader(http.StatusOK)
}

func runCommand(cmdstr string, args []string, dir string) (stdout, stderr string) {
	log.Println("Executing: " + cmdstr)
	cmd := exec.Command(cmdstr, args...)
	cmd.Dir = dir
	out, err := cmd.Output()
	if err != nil {
		stderr = err.Error()
	}
	stdout = string(out)
	return
}

func (app *App) postCommand(w http.ResponseWriter, r *http.Request) {
	log.Println("Request received!")

	response := simplejson.New()

	var command Command
	_ = json.NewDecoder(r.Body).Decode(&command)
	args := strings.Split(command.Args, " ")
	log.Println("dir", command.Dir)

	stdout, stderr := runCommand(command.Cmd, args,
		command.Dir)

	response.Set("out", stdout)
	response.Set("err", stderr)

	payload, err := response.MarshalJSON()
	if err != nil {
		log.Println(err)
	}

	w.Header().Set("Content-Type", "application/json")
	_, err = w.Write(payload)
	if err != nil {
		log.Fatalln(err)
	}
}

func main() {
	app := App{
		Router: mux.NewRouter().StrictSlash(true),
	}

	app.setupRouter()

	log.Fatal(http.ListenAndServe(":80", app.Router))
}
