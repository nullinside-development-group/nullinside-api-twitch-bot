pipeline {
    agent any
	options {
        ansiColor('xterm')
    }

    stages {
        stage('Checkout') {
            steps {
                git branch: env.BRANCH_NAME, credentialsId: 'GitHub PAT', url: 'https://github.com/nullinside-development-group/nullinside-api-twitch-bot.git'
            }
        }
        
        stage('Build & Deploy') {
            steps {
				withCredentials([
				    usernamePassword(credentialsId: 'GitHub PAT', passwordVariable: 'GITHUB_PASSWORD', usernameVariable: 'GITHUB_USERNAME'),
					usernamePassword(credentialsId: 'MySql', passwordVariable: 'MYSQL_PASSWORD', usernameVariable: 'MYSQL_USERNAME'),
					string(credentialsId: 'MySqlServer', variable: 'MYSQL_SERVER'),
					string(credentialsId: 'TwitchBotClientId', variable: 'TWITCH_BOT_CLIENT_ID'),
					string(credentialsId: 'TwitchBotClientSecret', variable: 'TWITCH_BOT_CLIENT_SECRET'),
					string(credentialsId: 'TwitchBotClientRedirect', variable: 'TWITCH_BOT_CLIENT_REDIRECT')
				]) {
					script {
						def statusCode = sh script: "bash go.sh", returnStatus:true
						if (statusCode != 0) {
							error "Build Failed"
						}
					}
				}
            }
        }
    }
	
	post {
		always {
			cleanWs cleanWhenFailure: false, notFailBuild: true
		}
	}
}
